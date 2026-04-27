namespace GitLabWebhook.Models
{
    /// <summary>
    /// Represents the result of parsing and validating a GitLab webhook payload.
    /// </summary>
    public class ParseResult
    {
        /// <summary>
        /// Indicates whether parsing and validation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The parsed and validated webhook event (null if validation failed).
        /// </summary>
        public GitLabIssueWebhookEvent Event { get; set; }

        /// <summary>
        /// List of validation errors (empty if successful).
        /// </summary>
        public List<ValidationError> Errors { get; set; }

        public ParseResult()
        {
            Success = false;
            Event = null;
            Errors = new List<ValidationError>();
        }

        /// <summary>
        /// Creates a successful parse result.
        /// </summary>
        public static ParseResult SuccessResult(GitLabIssueWebhookEvent evt)
        {
            return new ParseResult
            {
                Success = true,
                Event = evt,
                Errors = new List<ValidationError>()
            };
        }

        /// <summary>
        /// Creates a failed parse result with validation errors.
        /// </summary>
        public static ParseResult FailureResult(List<ValidationError> errors)
        {
            return new ParseResult
            {
                Success = false,
                Event = null,
                Errors = errors
            };
        }
    }
}
