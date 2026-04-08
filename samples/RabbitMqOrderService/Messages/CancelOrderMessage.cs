using System;

namespace RabbitMqOrderService.Messages
{
    /// <summary>
    /// Fire-and-forget message that replaces the WCF one-way <c>CancelOrder(string orderId)</c> operation.
    /// No reply queue or correlation ID is needed by the consumer.
    /// </summary>
    public class CancelOrderMessage
    {
        /// <summary>Unique identifier for tracing/idempotency purposes.</summary>
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        /// <summary>The order identifier to cancel.</summary>
        public string OrderId { get; set; }
    }
}
