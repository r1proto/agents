using System;

namespace RabbitMqOrderService.Messages
{
    /// <summary>
    /// Error reply message that replaces WCF's <c>FaultException&lt;ValidationFault&gt;</c>.
    /// Published to the client's reply queue in place of a normal response when an error occurs.
    /// </summary>
    public class ErrorReplyMessage
    {
        /// <summary>Echoes the CorrelationId of the originating request.</summary>
        public Guid CorrelationId { get; set; }

        /// <summary>The name of the invalid field, if the error is a validation failure.</summary>
        public string Field { get; set; }

        /// <summary>Human-readable description of the error.</summary>
        public string Reason { get; set; }

        /// <summary>
        /// Machine-readable error code.
        /// Convention: <c>"VALIDATION_ERROR"</c>, <c>"NOT_FOUND"</c>, <c>"INTERNAL_ERROR"</c>.
        /// </summary>
        public string ErrorCode { get; set; }
    }
}
