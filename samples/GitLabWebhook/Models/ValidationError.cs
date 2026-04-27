namespace GitLabWebhook.Models
{
    /// <summary>
    /// Represents a validation error for a specific field.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// The name of the field that failed validation.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Human-readable reason for the validation failure.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Machine-readable error code.
        /// </summary>
        public string ErrorCode { get; set; }

        public ValidationError()
        {
            Field = string.Empty;
            Reason = string.Empty;
            ErrorCode = string.Empty;
        }

        public ValidationError(string field, string reason, string errorCode)
        {
            Field = field;
            Reason = reason;
            ErrorCode = errorCode;
        }
    }
}
