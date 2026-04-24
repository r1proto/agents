using GitLabWebhook.Models;

namespace GitLabWebhook.Interfaces
{
    /// <summary>
    /// Forwards validated GitLab issue events to the downstream processing layer.
    /// </summary>
    public interface IIssueEventDispatcher
    {
        /// <summary>
        /// Dispatches a parsed GitLab issue event for further processing.
        /// </summary>
        /// <param name="issueEvent">The validated issue event to dispatch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DispatchAsync(GitLabIssueEvent issueEvent, CancellationToken cancellationToken = default);
    }
}
