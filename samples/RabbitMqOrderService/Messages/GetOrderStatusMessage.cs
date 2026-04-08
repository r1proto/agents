using System;

namespace RabbitMqOrderService.Messages
{
    /// <summary>
    /// RPC request message that replaces the WCF <c>GetOrderStatus(string orderId)</c> operation.
    /// </summary>
    public class GetOrderStatusMessage
    {
        /// <summary>Unique identifier used to correlate the response back to this request.</summary>
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        /// <summary>Name of the exclusive reply queue created by the client.</summary>
        public string ReplyTo { get; set; }

        /// <summary>The order identifier to look up.</summary>
        public string OrderId { get; set; }
    }
}
