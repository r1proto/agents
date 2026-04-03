using System;

namespace OrchestratedMigration.Messages
{
    /// <summary>
    /// Error reply DTO returned on the reply queue when a request fails validation.
    /// Maps WCF FaultException&lt;ValidationFault&gt; to a plain message.
    /// </summary>
    public class ErrorReplyMessage
    {
        /// <summary>Echo of the request CorrelationId so the client can match the error to its request.</summary>
        public Guid CorrelationId { get; set; }
        public string Field { get; set; }
        public string Reason { get; set; }
        public string ErrorCode { get; set; }
    }
}
