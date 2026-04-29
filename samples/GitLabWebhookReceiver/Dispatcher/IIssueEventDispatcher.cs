using System;
using GitLabWebhookReceiver.Models;
using GitLabWebhookReceiver.Submission;

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
    /// Production implementation of the issue event dispatcher.
    /// Converts GitLab issue events to agent tasks and submits them to the OpenCode AI Agent.
    /// </summary>
    public class AgentIssueEventDispatcher : IIssueEventDispatcher
    {
        private readonly IAgentSubmissionService _submissionService;
        private readonly GitLabIntegrationConfig _config;

        /// <summary>
        /// Creates a new instance of AgentIssueEventDispatcher.
        /// </summary>
        /// <param name="submissionService">The agent submission service to use</param>
        /// <param name="config">The GitLab integration configuration</param>
        public AgentIssueEventDispatcher(IAgentSubmissionService submissionService, GitLabIntegrationConfig config)
        {
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Validate configuration on initialization
            _config.Validate();

            if (!_config.Enabled)
            {
                Console.WriteLine("[Dispatcher] Warning: GitLab integration is disabled in configuration");
            }
        }

        /// <summary>
        /// Dispatches a validated GitLab issue event to the OpenCode AI Agent.
        /// </summary>
        public void DispatchIssueEvent(GitLabIssueEvent issueEvent)
        {
            if (issueEvent == null)
                throw new ArgumentNullException(nameof(issueEvent));

            // Check if integration is enabled
            if (!_config.Enabled)
            {
                Console.WriteLine("[Dispatcher] Skipping dispatch - integration is disabled");
                return;
            }

            var action = issueEvent.ObjectAttributes?.Action ?? "unknown";
            var title = issueEvent.ObjectAttributes?.Title ?? "untitled";
            var iid = issueEvent.ObjectAttributes?.Iid ?? 0;
            var projectPath = issueEvent.Project?.PathWithNamespace ?? "unknown";

            Console.WriteLine($"[Dispatcher] Dispatching issue event:");
            Console.WriteLine($"  Project: {projectPath}");
            Console.WriteLine($"  Issue #: {iid}");
            Console.WriteLine($"  Title: {title}");
            Console.WriteLine($"  Action: {action}");
            Console.WriteLine($"  Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            try
            {
                // Convert GitLab event to internal task schema
                var task = AgentTask.FromGitLabEvent(issueEvent, _config);

                Console.WriteLine($"[Dispatcher] Created agent task with dedup key: {task.GetDeduplicationKey()}");
                Console.WriteLine($"[Dispatcher] Target repository: {task.TargetRepoUrl}");

                // Submit task to agent
                var result = _submissionService.SubmitTask(task);

                // Handle submission result
                switch (result.ResultType)
                {
                    case SubmissionResultType.Success:
                        Console.WriteLine($"[Dispatcher] ✓ Task submitted successfully");
                        Console.WriteLine($"[Dispatcher]   Job ID: {result.AgentJobId}");
                        Console.WriteLine($"[Dispatcher]   Dedup key: {result.DeduplicationKey}");
                        break;

                    case SubmissionResultType.Duplicate:
                        Console.WriteLine($"[Dispatcher] ⊗ Duplicate task detected - skipping");
                        Console.WriteLine($"[Dispatcher]   Dedup key: {result.DeduplicationKey}");
                        break;

                    case SubmissionResultType.Failure:
                        Console.Error.WriteLine($"[Dispatcher] ✗ Task submission failed");
                        Console.Error.WriteLine($"[Dispatcher]   Error: {result.Message}");
                        Console.Error.WriteLine($"[Dispatcher]   Dedup key: {result.DeduplicationKey}");
                        if (result.Exception != null)
                        {
                            Console.Error.WriteLine($"[Dispatcher]   Exception: {result.Exception}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Dispatcher] Exception during dispatch: {ex.Message}");
                Console.Error.WriteLine($"[Dispatcher] Stack trace: {ex.StackTrace}");
                // Don't re-throw - we want to return a proper response to GitLab
            }
        }
    }

    /// <summary>
    /// Stub implementation of the issue event dispatcher.
    /// This is a placeholder that can be used for testing or when the agent submission is not configured.
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
