using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrchestratedMigration.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrchestratedMigration.Client
{
    /// <summary>
    /// RabbitMQ client that replaces the WCF OrderServiceClient (ClientBase&lt;IOrderService&gt;).
    /// RPC calls (PlaceOrder, GetOrderStatus) use the correlation-id pattern with a shared
    /// exclusive auto-delete reply queue. CancelOrder is fire-and-forget.
    /// </summary>
    public sealed class OrderServiceRabbitMqClient : IDisposable
    {
        private const string ExchangeName = "order-service";

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueue;
        private bool _disposed;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<PlaceOrderResponse>> _placeOrderPending
            = new ConcurrentDictionary<string, TaskCompletionSource<PlaceOrderResponse>>(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, TaskCompletionSource<GetOrderStatusResponse>> _statusPending
            = new ConcurrentDictionary<string, TaskCompletionSource<GetOrderStatusResponse>>(StringComparer.Ordinal);

        /// <summary>
        /// Creates the client, declares the reply queue, and starts the reply consumer.
        /// </summary>
        /// <param name="connection">An open <see cref="IConnection"/>.</param>
        public OrderServiceRabbitMqClient(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _channel = _connection.CreateModel();

            // Declare the exclusive, auto-delete reply queue shared by both RPC consumers.
            _replyQueue = _channel.QueueDeclare(queue: string.Empty, durable: false, exclusive: true, autoDelete: true).QueueName;

            var replyConsumer = new EventingBasicConsumer(_channel);
            replyConsumer.Received += OnReplyReceived;
            _channel.BasicConsume(_replyQueue, autoAck: true, consumer: replyConsumer);
        }

        // ── Reply dispatch ────────────────────────────────────────────────────────

        private void OnReplyReceived(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = Encoding.UTF8.GetString(e.Body.ToArray());

                // Attempt to read CorrelationId without knowing the type yet.
                var envelope = JsonConvert.DeserializeObject<CorrelationEnvelope>(body);
                if (envelope == null) return;

                var corrId = envelope.CorrelationId.ToString();

                // Check PlaceOrder pending first.
                if (_placeOrderPending.TryRemove(corrId, out var placeTcs))
                {
                    var response = JsonConvert.DeserializeObject<PlaceOrderResponse>(body);
                    placeTcs.TrySetResult(response);
                    return;
                }

                // Check GetOrderStatus pending.
                if (_statusPending.TryRemove(corrId, out var statusTcs))
                {
                    var response = JsonConvert.DeserializeObject<GetOrderStatusResponse>(body);
                    statusTcs.TrySetResult(response);
                    return;
                }

                // Unknown correlation — could be an error reply.
                Console.Error.WriteLine($"[Client] Received reply for unknown correlationId {corrId}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Client] Error processing reply: {ex.Message}");
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Sends a PlaceOrder request and awaits the response asynchronously.</summary>
        /// <param name="message">The request message. CorrelationId is set automatically if empty.</param>
        /// <returns>A <see cref="PlaceOrderResponse"/> from the consumer.</returns>
        public Task<PlaceOrderResponse> PlaceOrderAsync(PlaceOrderMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.CorrelationId == Guid.Empty) message.CorrelationId = Guid.NewGuid();
            message.ReplyTo = _replyQueue;

            var tcs = new TaskCompletionSource<PlaceOrderResponse>();
            _placeOrderPending[message.CorrelationId.ToString()] = tcs;

            Publish("place-order", message, persistent: true);
            return tcs.Task;
        }

        /// <summary>Sends a GetOrderStatus request and awaits the response asynchronously.</summary>
        /// <param name="message">The request message. CorrelationId is set automatically if empty.</param>
        /// <returns>A <see cref="GetOrderStatusResponse"/> from the consumer.</returns>
        public Task<GetOrderStatusResponse> GetOrderStatusAsync(GetOrderStatusMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.CorrelationId == Guid.Empty) message.CorrelationId = Guid.NewGuid();
            message.ReplyTo = _replyQueue;

            var tcs = new TaskCompletionSource<GetOrderStatusResponse>();
            _statusPending[message.CorrelationId.ToString()] = tcs;

            Publish("get-order-status", message, persistent: true);
            return tcs.Task;
        }

        /// <summary>Fire-and-forget CancelOrder publish. No reply is expected.</summary>
        /// <param name="message">The cancel message.</param>
        public void CancelOrder(CancelOrderMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Publish("cancel-order", message, persistent: true);
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private void Publish<T>(string routingKey, T payload, bool persistent)
        {
            var json = JsonConvert.SerializeObject(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            var props = _channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = (byte)(persistent ? 2 : 1);
            _channel.BasicPublish(ExchangeName, routingKey, props, bytes);
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        /// <summary>Releases all RabbitMQ resources and cancels pending RPC calls.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Cancel any in-flight RPCs.
                foreach (var tcs in _placeOrderPending.Values)
                    tcs.TrySetCanceled();
                foreach (var tcs in _statusPending.Values)
                    tcs.TrySetCanceled();

                _channel?.Close();
                _channel?.Dispose();
            }
            _disposed = true;
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private class CorrelationEnvelope
        {
            public Guid CorrelationId { get; set; }
        }
    }
}
