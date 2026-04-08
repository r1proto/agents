using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using OrchestratedMigration.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrchestratedMigration.Consumer
{
    /// <summary>
    /// RabbitMQ consumer that replaces the WCF ServiceHost + OrderServiceImpl.
    /// Declares the durable direct exchange and queue, binds all routing keys,
    /// and dispatches incoming messages to the appropriate handler.
    /// </summary>
    public sealed class OrderServiceConsumer : IDisposable
    {
        private const string ExchangeName = "order-service";
        private const string QueueName = "order-service";

        private static readonly Dictionary<string, OrderRecord> _orders =
            new Dictionary<string, OrderRecord>(StringComparer.Ordinal);

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private bool _disposed;

        /// <summary>Initialises the consumer, declares topology, and starts listening.</summary>
        /// <param name="factory">A configured <see cref="IConnectionFactory"/>.</param>
        public OrderServiceConsumer(IConnectionFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            DeclareTopology();
            StartConsuming();
        }

        // ── Topology ────────────────────────────────────────────────────────────

        private void DeclareTopology()
        {
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, "place-order");
            _channel.QueueBind(QueueName, ExchangeName, "get-order-status");
            _channel.QueueBind(QueueName, ExchangeName, "cancel-order");
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        private void StartConsuming()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;
            _channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);
        }

        // ── Dispatch ─────────────────────────────────────────────────────────────

        private void OnMessageReceived(object sender, BasicDeliverEventArgs e)
        {
            string replyTo = null;
            Guid correlationId = Guid.Empty;

            try
            {
                var body = Encoding.UTF8.GetString(e.Body.ToArray());

                switch (e.RoutingKey)
                {
                    case "place-order":
                        var placeMsg = JsonConvert.DeserializeObject<PlaceOrderMessage>(body);
                        replyTo = placeMsg.ReplyTo;
                        correlationId = placeMsg.CorrelationId;
                        HandlePlaceOrder(placeMsg);
                        break;

                    case "get-order-status":
                        var statusMsg = JsonConvert.DeserializeObject<GetOrderStatusMessage>(body);
                        replyTo = statusMsg.ReplyTo;
                        correlationId = statusMsg.CorrelationId;
                        HandleGetOrderStatus(statusMsg);
                        break;

                    case "cancel-order":
                        var cancelMsg = JsonConvert.DeserializeObject<CancelOrderMessage>(body);
                        correlationId = cancelMsg.CorrelationId;
                        HandleCancelOrder(cancelMsg);
                        break;

                    default:
                        Console.Error.WriteLine($"[Consumer] Unknown routing key: {e.RoutingKey}");
                        break;
                }

                _channel.BasicAck(e.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Consumer] ERROR processing '{e.RoutingKey}': {ex.Message}");

                if (!string.IsNullOrEmpty(replyTo))
                {
                    PublishError(replyTo, correlationId, ex.Message);
                }

                _channel.BasicNack(e.DeliveryTag, multiple: false, requeue: false);
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────────

        /// <summary>Handles a PlaceOrder request: validates, computes total, stores order, replies.</summary>
        private void HandlePlaceOrder(PlaceOrderMessage msg)
        {
            if (string.IsNullOrWhiteSpace(msg.CustomerId))
                throw new ArgumentException("CustomerId is required.", nameof(msg.CustomerId));

            if (msg.Lines == null || msg.Lines.Count == 0)
                throw new ArgumentException("At least one order line is required.", nameof(msg.Lines));

            var orderId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            decimal total = 0m;
            foreach (var line in msg.Lines)
                total += line.Quantity * line.UnitPrice;

            lock (_orders)
            {
                _orders[orderId] = new OrderRecord
                {
                    OrderId = orderId,
                    Status = "Pending",
                    TrackingNumber = null
                };
            }

            Console.WriteLine($"[Consumer] PlaceOrder: orderId={orderId}, customer={msg.CustomerId}, total={total:C}");

            var response = new PlaceOrderResponse
            {
                CorrelationId = msg.CorrelationId,
                Success = true,
                OrderId = orderId,
                TotalAmount = total,
                Message = "Order placed successfully."
            };

            Publish(msg.ReplyTo, response);
        }

        /// <summary>Handles a GetOrderStatus request: looks up the order, replies with current status.</summary>
        private void HandleGetOrderStatus(GetOrderStatusMessage msg)
        {
            OrderRecord record;
            lock (_orders)
            {
                if (!_orders.TryGetValue(msg.OrderId, out record))
                    throw new InvalidOperationException($"Order '{msg.OrderId}' not found.");
            }

            Console.WriteLine($"[Consumer] GetOrderStatus: orderId={msg.OrderId}, status={record.Status}");

            var response = new GetOrderStatusResponse
            {
                CorrelationId = msg.CorrelationId,
                OrderId = record.OrderId,
                Status = record.Status,
                TrackingNumber = record.TrackingNumber
            };

            Publish(msg.ReplyTo, response);
        }

        /// <summary>Handles a CancelOrder fire-and-forget request: sets status to Cancelled, no reply.</summary>
        private void HandleCancelOrder(CancelOrderMessage msg)
        {
            if (string.IsNullOrWhiteSpace(msg.OrderId))
            {
                Console.WriteLine("[Consumer] CancelOrder called with null/empty orderId — ignoring.");
                return;
            }

            lock (_orders)
            {
                if (_orders.TryGetValue(msg.OrderId, out var record))
                {
                    record.Status = "Cancelled";
                    Console.WriteLine($"[Consumer] CancelOrder: orderId={msg.OrderId} cancelled.");
                }
                else
                {
                    Console.WriteLine($"[Consumer] CancelOrder: orderId={msg.OrderId} not found — ignoring.");
                }
            }
        }

        // ── Publishing helpers ────────────────────────────────────────────────────

        private void Publish<T>(string replyQueue, T payload)
        {
            if (string.IsNullOrEmpty(replyQueue)) return;

            var json = JsonConvert.SerializeObject(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            var props = _channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2;
            _channel.BasicPublish(exchange: string.Empty, routingKey: replyQueue, basicProperties: props, body: bytes);
        }

        private void PublishError(string replyQueue, Guid correlationId, string message)
        {
            var error = new ErrorReplyMessage
            {
                CorrelationId = correlationId,
                ErrorCode = "INTERNAL_ERROR",
                Field = string.Empty,
                Reason = message
            };
            Publish(replyQueue, error);
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        /// <summary>Releases all RabbitMQ resources.</summary>
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
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>Internal order state record stored in memory.</summary>
    internal class OrderRecord
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
        public string TrackingNumber { get; set; }
    }
}
