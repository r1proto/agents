using System;

namespace OrchestratedMigration.Messages
{
    /// <summary>Reply DTO for the PlaceOrder operation. Replaces WCF OrderConfirmation.</summary>
    public class PlaceOrderResponse
    {
        /// <summary>Echo of the request CorrelationId for RPC matching.</summary>
        public Guid CorrelationId { get; set; }
        public bool Success { get; set; }
        public string OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Message { get; set; }
    }
}
