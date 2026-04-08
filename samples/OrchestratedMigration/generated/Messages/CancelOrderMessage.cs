using System;

namespace OrchestratedMigration.Messages
{
    /// <summary>
    /// Fire-and-forget message DTO for the CancelOrder operation.
    /// No ReplyTo — maps to WCF IsOneWay=true.
    /// </summary>
    public class CancelOrderMessage
    {
        /// <summary>Envelope field for tracing purposes.</summary>
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        /// <remarks>Mandatory field.</remarks>
        public string OrderId { get; set; }
    }
}
