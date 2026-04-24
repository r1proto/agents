using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqOrderService.Messages;

namespace RabbitMqOrderService.Consumer
{
    /// <summary>
    /// Pure business logic extracted from the WCF <c>OrderServiceImpl</c>.
    /// Keeping it in a separate class allows it to be unit-tested without any RabbitMQ dependency.
    /// </summary>
    public class OrderLogic
    {
        private readonly Dictionary<string, OrderState> _orders =
            new Dictionary<string, OrderState>(StringComparer.Ordinal);

        /// <summary>
        /// Processes a <see cref="PlaceOrderMessage"/> and returns a <see cref="PlaceOrderResponse"/>.
        /// Returns an error response instead of throwing when validation fails.
        /// </summary>
        public PlaceOrderResponse HandlePlaceOrder(PlaceOrderMessage msg)
        {
            if (msg == null)
                return ErrorResponse<PlaceOrderResponse>(Guid.Empty, "Request", "Request cannot be null.", "VALIDATION_ERROR");

            if (string.IsNullOrWhiteSpace(msg.CustomerId))
                return ErrorResponse<PlaceOrderResponse>(msg.CorrelationId, "CustomerId", "CustomerId is required.", "VALIDATION_ERROR");

            if (msg.Lines == null || msg.Lines.Count == 0)
                return ErrorResponse<PlaceOrderResponse>(msg.CorrelationId, "Lines", "At least one order line is required.", "VALIDATION_ERROR");

            var orderId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            decimal total = 0;
            foreach (var line in msg.Lines)
                total += line.Quantity * line.UnitPrice;

            _orders[orderId] = new OrderState { OrderId = orderId, Status = "Pending", TrackingNumber = null };

            Console.WriteLine($"[OrderLogic] PlaceOrder: orderId={orderId}, customer={msg.CustomerId}, total={total:C}");

            return new PlaceOrderResponse
            {
                CorrelationId = msg.CorrelationId,
                Success       = true,
                OrderId       = orderId,
                TotalAmount   = total,
                Message       = "Order placed successfully."
            };
        }

        /// <summary>
        /// Processes a <see cref="GetOrderStatusMessage"/> and returns a <see cref="GetOrderStatusResponse"/>
        /// or an <see cref="ErrorReplyMessage"/> serialised into the response's <c>Message</c> field
        /// when the order is not found (the consumer sends it as an ErrorReplyMessage instead).
        /// </summary>
        public (GetOrderStatusResponse response, ErrorReplyMessage error) HandleGetOrderStatus(GetOrderStatusMessage msg)
        {
            if (msg == null)
                return (null, new ErrorReplyMessage { CorrelationId = Guid.Empty, Field = "Request", Reason = "Request cannot be null.", ErrorCode = "VALIDATION_ERROR" });

            if (string.IsNullOrWhiteSpace(msg.OrderId))
                return (null, new ErrorReplyMessage { CorrelationId = msg.CorrelationId, Field = "OrderId", Reason = "OrderId cannot be null or empty.", ErrorCode = "VALIDATION_ERROR" });

            if (!_orders.TryGetValue(msg.OrderId, out var state))
                return (null, new ErrorReplyMessage { CorrelationId = msg.CorrelationId, Field = "OrderId", Reason = $"Order '{msg.OrderId}' not found.", ErrorCode = "NOT_FOUND" });

            Console.WriteLine($"[OrderLogic] GetOrderStatus: orderId={msg.OrderId}, status={state.Status}");

            return (new GetOrderStatusResponse
            {
                CorrelationId  = msg.CorrelationId,
                OrderId        = state.OrderId,
                Status         = state.Status,
                TrackingNumber = state.TrackingNumber
            }, null);
        }

        /// <summary>
        /// Processes a <see cref="CancelOrderMessage"/>. Fire-and-forget — no return value.
        /// </summary>
        public void HandleCancelOrder(CancelOrderMessage msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.OrderId))
            {
                Console.WriteLine("[OrderLogic] CancelOrder called with null/empty orderId — ignoring.");
                return;
            }

            if (_orders.TryGetValue(msg.OrderId, out var state))
            {
                state.Status = "Cancelled";
                Console.WriteLine($"[OrderLogic] CancelOrder: orderId={msg.OrderId} cancelled.");
            }
            else
            {
                Console.WriteLine($"[OrderLogic] CancelOrder: orderId={msg.OrderId} not found — ignoring.");
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────────────

        private static T ErrorResponse<T>(Guid correlationId, string field, string reason, string errorCode)
            where T : class, new()
        {
            // Only PlaceOrderResponse is returned this way; for richer typing a union type could be used.
            if (typeof(T) == typeof(PlaceOrderResponse))
            {
                return new PlaceOrderResponse
                {
                    CorrelationId = correlationId,
                    Success       = false,
                    Message       = $"{field}: {reason}"
                } as T;
            }
            return null;
        }

        /// <summary>Internal order state record.</summary>
        internal class OrderState
        {
            public string OrderId        { get; set; }
            public string Status         { get; set; }
            public string TrackingNumber { get; set; }
        }
    }

    /// <summary>
    /// Replaces WCF's <c>ServiceHost + OrderServiceImpl</c>.
    /// Declares the AMQP topology, subscribes to the <c>order-service</c> queue,
    /// dispatches messages by routing key, and publishes RPC replies.
    /// </summary>
    public sealed class OrderServiceConsumer : IDisposable
    {
        /// <summary>Exchange and queue name shared by all routing keys.</summary>
        public const string ExchangeName = "order-service";

        /// <summary>Durable queue that receives all order-related messages.</summary>
        public const string QueueName = "order-service";

        public const string RoutingKeyPlaceOrder      = "place-order";
        public const string RoutingKeyGetOrderStatus  = "get-order-status";
        public const string RoutingKeyCancelOrder     = "cancel-order";

        private readonly IConnection _connection;
        private readonly IModel      _channel;
        private readonly OrderLogic  _logic = new OrderLogic();
        private bool _disposed;

        /// <summary>
        /// Initialises the consumer, declares the exchange/queue topology, and begins consuming.
        /// </summary>
        /// <param name="factory">An open-able <see cref="IConnectionFactory"/>.</param>
        public OrderServiceConsumer(IConnectionFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _connection = factory.CreateConnection();
            _channel    = _connection.CreateModel();

            DeclareTopology();
            StartConsuming();
        }

        // ── topology ─────────────────────────────────────────────────────────────────

        private void DeclareTopology()
        {
            _channel.ExchangeDeclare(
                exchange:    ExchangeName,
                type:        ExchangeType.Direct,
                durable:     true,
                autoDelete:  false,
                arguments:   null);

            _channel.QueueDeclare(
                queue:       QueueName,
                durable:     true,
                exclusive:   false,
                autoDelete:  false,
                arguments:   null);

            foreach (var key in new[] { RoutingKeyPlaceOrder, RoutingKeyGetOrderStatus, RoutingKeyCancelOrder })
            {
                _channel.QueueBind(QueueName, ExchangeName, key);
            }

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 16, global: false);
        }

        // ── consumer ─────────────────────────────────────────────────────────────────

        private void StartConsuming()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;
            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            Console.WriteLine($"[OrderServiceConsumer] Listening on queue '{QueueName}'.");
        }

        private void OnMessageReceived(object sender, BasicDeliverEventArgs e)
        {
            var routingKey = e.RoutingKey;

            try
            {
                switch (routingKey)
                {
                    case RoutingKeyPlaceOrder:
                        HandlePlaceOrder(e);
                        break;

                    case RoutingKeyGetOrderStatus:
                        HandleGetOrderStatus(e);
                        break;

                    case RoutingKeyCancelOrder:
                        HandleCancelOrder(e);
                        break;

                    default:
                        Console.Error.WriteLine($"[OrderServiceConsumer] Unknown routing key '{routingKey}' — nacking without requeue.");
                        _channel.BasicNack(e.DeliveryTag, multiple: false, requeue: false);
                        return;
                }

                _channel.BasicAck(e.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OrderServiceConsumer] Unhandled exception on routing key '{routingKey}': {ex}");
                _channel.BasicNack(e.DeliveryTag, multiple: false, requeue: false);
            }
        }

        // ── handlers ─────────────────────────────────────────────────────────────────

        private void HandlePlaceOrder(BasicDeliverEventArgs e)
        {
            var msg      = Deserialize<PlaceOrderMessage>(e.Body);
            var response = _logic.HandlePlaceOrder(msg);
            var replyTo  = msg?.ReplyTo;

            if (!string.IsNullOrEmpty(replyTo))
                PublishReply(replyTo, response, msg?.CorrelationId ?? Guid.Empty);
        }

        private void HandleGetOrderStatus(BasicDeliverEventArgs e)
        {
            var msg = Deserialize<GetOrderStatusMessage>(e.Body);
            var (response, error) = _logic.HandleGetOrderStatus(msg);
            var replyTo = msg?.ReplyTo;

            if (!string.IsNullOrEmpty(replyTo))
            {
                if (error != null)
                    PublishReply(replyTo, error, msg?.CorrelationId ?? Guid.Empty);
                else
                    PublishReply(replyTo, response, msg?.CorrelationId ?? Guid.Empty);
            }
        }

        private void HandleCancelOrder(BasicDeliverEventArgs e)
        {
            var msg = Deserialize<CancelOrderMessage>(e.Body);
            _logic.HandleCancelOrder(msg);
            // One-way: no reply published.
        }

        // ── reply helpers ─────────────────────────────────────────────────────────────

        private void PublishReply<T>(string replyTo, T payload, Guid correlationId)
        {
            var json  = JsonConvert.SerializeObject(payload);
            var body  = Encoding.UTF8.GetBytes(json);
            var props = _channel.CreateBasicProperties();
            props.ContentType  = "application/json";
            props.CorrelationId = correlationId.ToString();

            // Publish to the default exchange; routing key == queue name (the temp reply queue).
            _channel.BasicPublish(exchange: "", routingKey: replyTo, basicProperties: props, body: body);
        }

        private static T Deserialize<T>(System.ReadOnlyMemory<byte> body)
        {
            var json = Encoding.UTF8.GetString(body.ToArray());
            return JsonConvert.DeserializeObject<T>(json);
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
