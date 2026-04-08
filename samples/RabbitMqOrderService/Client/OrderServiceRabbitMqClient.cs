using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqOrderService.Messages;

namespace RabbitMqOrderService.Client
{
    /// <summary>
    /// Replaces the WCF <c>OrderServiceClient</c> (ClientBase&lt;IOrderService&gt;).
    /// Uses the RabbitMQ RPC pattern for request-reply operations and fire-and-forget for
    /// one-way operations.
    /// </summary>
    public sealed class OrderServiceRabbitMqClient : IDisposable
    {
        private const string ExchangeName = "order-service";

        private readonly IConnection _connection;
        private readonly IModel      _channel;

        // Each RPC operation type gets its own exclusive reply queue and pending-call dictionary.
        private readonly string _placeOrderReplyQueue;
        private readonly string _getStatusReplyQueue;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<PlaceOrderResponse>>
            _placeOrderPending = new ConcurrentDictionary<string, TaskCompletionSource<PlaceOrderResponse>>();

        private readonly ConcurrentDictionary<string, TaskCompletionSource<GetOrderStatusResponse>>
            _getStatusPending = new ConcurrentDictionary<string, TaskCompletionSource<GetOrderStatusResponse>>();

        private bool _disposed;

        /// <summary>
        /// Creates a client connected to RabbitMQ via the supplied factory.
        /// Declares exclusive reply queues and wires up reply consumers.
        /// </summary>
        /// <param name="factory">Pre-configured <see cref="IConnectionFactory"/>.</param>
        public OrderServiceRabbitMqClient(IConnectionFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _connection = factory.CreateConnection();
            _channel    = _connection.CreateModel();

            _placeOrderReplyQueue = _channel.QueueDeclare(exclusive: true).QueueName;
            _getStatusReplyQueue  = _channel.QueueDeclare(exclusive: true).QueueName;

            WireReplyConsumer<PlaceOrderResponse>(_placeOrderReplyQueue, _placeOrderPending);
            WireReplyConsumer<GetOrderStatusResponse>(_getStatusReplyQueue, _getStatusPending);
        }

        // ── public API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a <see cref="PlaceOrderMessage"/> and awaits the <see cref="PlaceOrderResponse"/>.
        /// </summary>
        /// <param name="message">The order to place.</param>
        /// <param name="timeout">How long to wait for a reply (default: 30 s).</param>
        public async Task<PlaceOrderResponse> PlaceOrderAsync(
            PlaceOrderMessage message,
            TimeSpan? timeout = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.ReplyTo = _placeOrderReplyQueue;
            return await SendRpcAsync(
                message,
                message.CorrelationId,
                Consumer.OrderServiceConsumer.RoutingKeyPlaceOrder,
                _placeOrderPending,
                timeout ?? TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Sends a <see cref="GetOrderStatusMessage"/> and awaits the <see cref="GetOrderStatusResponse"/>.
        /// </summary>
        /// <param name="message">The status query.</param>
        /// <param name="timeout">How long to wait for a reply (default: 30 s).</param>
        public async Task<GetOrderStatusResponse> GetOrderStatusAsync(
            GetOrderStatusMessage message,
            TimeSpan? timeout = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.ReplyTo = _getStatusReplyQueue;
            return await SendRpcAsync(
                message,
                message.CorrelationId,
                Consumer.OrderServiceConsumer.RoutingKeyGetOrderStatus,
                _getStatusPending,
                timeout ?? TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Publishes a <see cref="CancelOrderMessage"/> and returns immediately (fire-and-forget).
        /// Mirrors the WCF one-way <c>CancelOrder</c> operation.
        /// </summary>
        /// <param name="message">The cancellation request.</param>
        public void CancelOrder(CancelOrderMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Publish(message, Consumer.OrderServiceConsumer.RoutingKeyCancelOrder, replyTo: null, correlationId: message.CorrelationId);
        }

        // ── internals ─────────────────────────────────────────────────────────────────

        private async Task<TReply> SendRpcAsync<TRequest, TReply>(
            TRequest request,
            Guid correlationId,
            string routingKey,
            ConcurrentDictionary<string, TaskCompletionSource<TReply>> pending,
            TimeSpan timeout)
            where TReply : class
        {
            var tcs = new TaskCompletionSource<TReply>(TaskCreationOptions.RunContinuationsAsynchronously);
            var key = correlationId.ToString();
            pending[key] = tcs;

            try
            {
                // Discover which reply queue to pass as replyTo by reflection — we already set it on the message.
                string replyTo = null;
                var prop = request.GetType().GetProperty("ReplyTo");
                if (prop != null) replyTo = prop.GetValue(request) as string;

                Publish(request, routingKey, replyTo, correlationId);

                using (var cts = new CancellationTokenSource(timeout))
                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                pending.TryRemove(key, out _);
            }
        }

        private void Publish<T>(T payload, string routingKey, string replyTo, Guid correlationId)
        {
            var json  = JsonConvert.SerializeObject(payload);
            var body  = Encoding.UTF8.GetBytes(json);
            var props = _channel.CreateBasicProperties();
            props.ContentType   = "application/json";
            props.CorrelationId = correlationId.ToString();
            if (replyTo != null) props.ReplyTo = replyTo;

            _channel.BasicPublish(
                exchange:       ExchangeName,
                routingKey:     routingKey,
                basicProperties: props,
                body:           body);
        }

        private void WireReplyConsumer<TReply>(
            string queueName,
            ConcurrentDictionary<string, TaskCompletionSource<TReply>> pending)
            where TReply : class
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (_, e) =>
            {
                var key = e.BasicProperties?.CorrelationId;
                if (key != null && pending.TryRemove(key, out var tcs))
                {
                    try
                    {
                        var json  = Encoding.UTF8.GetString(e.Body.ToArray());
                        var reply = JsonConvert.DeserializeObject<TReply>(json);
                        tcs.TrySetResult(reply);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
            };
            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }

        // ── IDisposable ───────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}
