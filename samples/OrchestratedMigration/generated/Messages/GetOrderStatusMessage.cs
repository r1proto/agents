using System;

namespace OrchestratedMigration.Messages
{
    /// <summary>Request DTO for the GetOrderStatus operation.</summary>
    public class GetOrderStatusMessage
    {
        /// <summary>Envelope field for RPC correlation.</summary>
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        /// <summary>Reply queue name for RPC pattern.</summary>
        public string ReplyTo { get; set; }
        /// <remarks>Mandatory field.</remarks>
        public string OrderId { get; set; }
    }
}
