using System;

namespace RabbitMqOrderService.Messages
{
    /// <summary>
    /// RPC response message that replaces the WCF <c>OrderConfirmation</c> DataContract.
    /// Published by the consumer to the client's reply queue.
    /// </summary>
    public class PlaceOrderResponse
    {
        /// <summary>Echoes the <see cref="PlaceOrderMessage.CorrelationId"/> of the originating request.</summary>
        public Guid CorrelationId { get; set; }

        /// <summary><c>true</c> when the order was accepted; <c>false</c> when validation failed.</summary>
        public bool Success { get; set; }

        /// <summary>System-assigned order identifier, populated on success.</summary>
        public string OrderId { get; set; }

        /// <summary>Sum of (Quantity × UnitPrice) across all order lines.</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Human-readable outcome message.</summary>
        public string Message { get; set; }
    }
}
