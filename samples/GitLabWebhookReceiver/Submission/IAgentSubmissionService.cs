using System;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Submission
{
    /// <summary>
    /// Represents the result of an agent submission operation.
    /// </summary>
    public enum SubmissionResultType
    {
        /// <summary>Task was successfully submitted to the agent</summary>
        Success,

        /// <summary>Task was a duplicate and not resubmitted</summary>
        Duplicate,

        /// <summary>Submission failed due to an error</summary>
        Failure
    }

    /// <summary>
    /// Represents the result of an agent submission operation.
    /// </summary>
    public class SubmissionResult
    {
        /// <summary>The type of result</summary>
        public SubmissionResultType ResultType { get; set; }

        /// <summary>Optional message providing details about the result</summary>
        public string Message { get; set; }

        /// <summary>The deduplication key used for this submission</summary>
        public string DeduplicationKey { get; set; }

        /// <summary>Optional agent job/run ID if the submission was successful</summary>
        public string AgentJobId { get; set; }

        /// <summary>Optional exception if the submission failed</summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Creates a successful submission result.
        /// </summary>
        public static SubmissionResult Success(string deduplicationKey, string agentJobId = null, string message = null)
        {
            return new SubmissionResult
            {
                ResultType = SubmissionResultType.Success,
                DeduplicationKey = deduplicationKey,
                AgentJobId = agentJobId,
                Message = message ?? "Task successfully submitted to agent"
            };
        }

        /// <summary>
        /// Creates a duplicate submission result.
        /// </summary>
        public static SubmissionResult Duplicate(string deduplicationKey, string message = null)
        {
            return new SubmissionResult
            {
                ResultType = SubmissionResultType.Duplicate,
                DeduplicationKey = deduplicationKey,
                Message = message ?? "Task was already submitted (duplicate detected)"
            };
        }

        /// <summary>
        /// Creates a failure submission result.
        /// </summary>
        public static SubmissionResult Failure(string deduplicationKey, string message, Exception exception = null)
        {
            return new SubmissionResult
            {
                ResultType = SubmissionResultType.Failure,
                DeduplicationKey = deduplicationKey,
                Message = message,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Interface for submitting tasks to the OpenCode AI Agent.
    /// Implementations should handle deduplication, validation, and actual submission.
    /// </summary>
    public interface IAgentSubmissionService
    {
        /// <summary>
        /// Submits a task to the OpenCode AI Agent.
        /// Performs idempotency checks to avoid duplicate submissions.
        /// </summary>
        /// <param name="task">The agent task to submit</param>
        /// <returns>A submission result indicating success, duplicate, or failure</returns>
        SubmissionResult SubmitTask(AgentTask task);
    }
}
