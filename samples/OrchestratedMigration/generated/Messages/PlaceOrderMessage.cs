using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrchestratedMigration.Messages
{
    /// <summary>Replaces WCF OrderRequest + PlaceOrder OperationContract.</summary>
    public class PlaceOrderMessage
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        /// <remarks>Mandatory field.</remarks>
        public string ReplyTo { get; set; }
        /// <remarks>Mandatory field.</remarks>
        public string CustomerId { get; set; }
        /// <remarks>Mandatory field.</remarks>
        public List<OrderLineDto> Lines { get; set; }
        public string ShippingAddress { get; set; }
    }

    public class OrderLineDto
    {
        /// <remarks>Mandatory field.</remarks>
        public string ProductId { get; set; }
        /// <remarks>Mandatory field.</remarks>
        public int Quantity { get; set; }
        /// <remarks>Mandatory field.</remarks>
        public decimal UnitPrice { get; set; }
    }
}
