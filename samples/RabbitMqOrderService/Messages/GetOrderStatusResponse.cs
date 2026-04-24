using System;

namespace RabbitMqOrderService.Messages
{
    /// <summary>
    /// RPC response message that replaces the WCF <c>OrderStatus</c> DataContract.
    /// </summary>
    public class GetOrderStatusResponse
    {
        /// <summary>Echoes the <see cref="GetOrderStatusMessage.CorrelationId"/> of the originating request.</summary>
        public Guid CorrelationId { get; set; }

        /// <summary>The order identifier that was queried.</summary>
        public string OrderId { get; set; }

        /// <summary>Current order status: Pending, Processing, Shipped, Delivered, or Cancelled.</summary>
        public string Status { get; set; }

        /// <summary>Carrier tracking number, or <c>null</c> if not yet shipped.</summary>
        public string TrackingNumber { get; set; }
    }
}
