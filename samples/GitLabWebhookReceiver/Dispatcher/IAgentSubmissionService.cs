using System;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Dispatcher
{
    /// <summary>
    /// Provider-agnostic interface for submitting tasks to the OpenCode AI Agent.
    /// This interface is independent of GitLab or any other event source.
    /// </summary>
    public interface IAgentSubmissionService
    {
        /// <summary>
        /// Submits a task to the agent for execution.
        /// </summary>
        /// <param name="task">The task to submit</param>
        /// <returns>A result indicating success or failure</returns>
        AgentSubmissionResult SubmitTask(AgentTask task);
    }

    /// <summary>
    /// Result of an agent task submission.
    /// </summary>
    public class AgentSubmissionResult
    {
        /// <summary>Indicates whether the submission was successful</summary>
        public bool Success { get; set; }

        /// <summary>Error message if submission failed</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Optional agent execution/task ID if submission succeeded</summary>
        public string TaskId { get; set; }

        public static AgentSubmissionResult Successful(string taskId = null)
        {
            return new AgentSubmissionResult
            {
                Success = true,
                TaskId = taskId
            };
        }

        public static AgentSubmissionResult Failed(string errorMessage)
        {
            return new AgentSubmissionResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Stub implementation of the agent submission service for testing and development.
    /// Replace with actual agent integration when available.
    /// </summary>
    public class StubAgentSubmissionService : IAgentSubmissionService
    {
        public AgentSubmissionResult SubmitTask(AgentTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // Simulate successful submission
            Console.WriteLine($"[StubAgentSubmission] Submitting task to agent:");
            Console.WriteLine($"  Source: {task.Source}");
            Console.WriteLine($"  Issue: {task.SourceProjectPath}#{task.IssueIid}");
            Console.WriteLine($"  Title: {task.Title}");
            Console.WriteLine($"  Target Repo: {task.TargetRepoUrl}");
            Console.WriteLine($"  Target Ref: {task.TargetRepoRef}");

            // Generate a fake task ID
            var taskId = $"task-{Guid.NewGuid().ToString().Substring(0, 8)}";
            return AgentSubmissionResult.Successful(taskId);
        }
    }
}
