using System;
using System.Collections.Generic;

namespace RabbitMqOrderService.Messages
{
    /// <summary>
    /// RPC request message that replaces the WCF <c>PlaceOrder(OrderRequest)</c> operation.
    /// The consumer publishes its response to the queue named by <see cref="ReplyTo"/>.
    /// </summary>
    public class PlaceOrderMessage
    {
        /// <summary>Unique identifier used to correlate the response back to this request.</summary>
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        /// <summary>Name of the exclusive reply queue created by the client (amq.rabbitmq.reply-to or a temp queue).</summary>
        public string ReplyTo { get; set; }

        /// <summary>Identifier of the customer placing the order.</summary>
        public string CustomerId { get; set; }

        /// <summary>One or more order lines.</summary>
        public List<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();

        /// <summary>Delivery address for the order.</summary>
        public string ShippingAddress { get; set; }
    }

    /// <summary>
    /// Represents a single line in an order, replacing the WCF <c>OrderLine</c> DataContract.
    /// </summary>
    public class OrderLineDto
    {
        /// <summary>Catalogue identifier for the product.</summary>
        public string ProductId { get; set; }

        /// <summary>Number of units ordered.</summary>
        public int Quantity { get; set; }

        /// <summary>Price per unit at the time of ordering.</summary>
        public decimal UnitPrice { get; set; }
    }
}
