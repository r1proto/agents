using System;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Dispatcher
{
    /// <summary>
    /// Interface for dispatching validated GitLab issue events to downstream processing.
    /// The webhook receiver forwards events to this layer after validation.
    /// </summary>
    public interface IIssueEventDispatcher
    {
        /// <summary>
        /// Dispatches a validated GitLab issue event for further processing.
        /// </summary>
        /// <param name="issueEvent">The validated GitLab issue event</param>
        void DispatchIssueEvent(GitLabIssueEvent issueEvent);
    }

    /// <summary>
    /// Stub implementation of the issue event dispatcher.
    /// This is a placeholder for the actual dispatcher implementation (issue r1proto/agents#3).
    /// Replace this with the real dispatcher when available.
    /// </summary>
    public class StubIssueEventDispatcher : IIssueEventDispatcher
    {
        /// <summary>
        /// Logs the issue event to console. Replace with actual dispatch logic.
        /// </summary>
        public void DispatchIssueEvent(GitLabIssueEvent issueEvent)
        {
            if (issueEvent == null)
                throw new ArgumentNullException(nameof(issueEvent));

            var action = issueEvent.ObjectAttributes?.Action ?? "unknown";
            var title = issueEvent.ObjectAttributes?.Title ?? "untitled";
            var iid = issueEvent.ObjectAttributes?.Iid ?? 0;
            var projectPath = issueEvent.Project?.PathWithNamespace ?? "unknown";

            Console.WriteLine($"[StubDispatcher] Dispatching issue event:");
            Console.WriteLine($"  Project: {projectPath}");
            Console.WriteLine($"  Issue #: {iid}");
            Console.WriteLine($"  Title: {title}");
            Console.WriteLine($"  Action: {action}");
            Console.WriteLine($"  Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        }
    }
}
