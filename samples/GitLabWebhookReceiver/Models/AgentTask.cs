using System;
using System.Collections.Generic;
using System.Linq;

namespace GitLabWebhookReceiver.Models
{
    /// <summary>
    /// Represents an internal task schema for dispatching GitLab issues to the OpenCode AI Agent.
    /// This schema is provider-agnostic at the agent layer and separates source (GitLab) from target (codebase).
    /// </summary>
    public class AgentTask
    {
        // Source - GitLab issue metadata

        /// <summary>Source system identifier (fixed value: "gitlab")</summary>
        public string Source { get; set; }

        /// <summary>Base URL of the GitLab instance</summary>
        public string InstanceUrl { get; set; }

        /// <summary>Numeric ID of the GitLab project where the issue is filed</summary>
        public int SourceProjectId { get; set; }

        /// <summary>Full path of the GitLab project (e.g. "group/subgroup/tracker")</summary>
        public string SourceProjectPath { get; set; }

        /// <summary>Project-scoped issue identifier</summary>
        public int IssueIid { get; set; }

        /// <summary>Issue title</summary>
        public string Title { get; set; }

        /// <summary>Issue body/description</summary>
        public string Description { get; set; }

        /// <summary>List of label names</summary>
        public List<string> Labels { get; set; }

        /// <summary>Issue author username</summary>
        public string Author { get; set; }

        /// <summary>List of assignee usernames</summary>
        public List<string> Assignees { get; set; }

        /// <summary>Issue state (opened, closed, etc.)</summary>
        public string State { get; set; }

        /// <summary>Human-visible URL of the GitLab issue</summary>
        public string WebUrl { get; set; }

        /// <summary>Webhook action that triggered dispatch (open, update, etc.)</summary>
        public string EventAction { get; set; }

        /// <summary>ISO-8601 timestamp of the triggering event</summary>
        public DateTime EventTimestamp { get; set; }

        // Target - codebase the agent should work on

        /// <summary>URL of the repository the agent should clone/check out and modify (REQUIRED)</summary>
        public string TargetRepoUrl { get; set; }

        /// <summary>Branch or ref the agent should start from (optional; defaults to default branch)</summary>
        public string TargetRepoRef { get; set; }

        /// <summary>
        /// Default constructor for AgentTask.
        /// Initializes collection properties to empty lists to avoid null reference issues.
        /// </summary>
        public AgentTask()
        {
            Source = "gitlab";
            Labels = new List<string>();
            Assignees = new List<string>();
            Description = string.Empty;
            TargetRepoRef = string.Empty;
        }

        /// <summary>
        /// Creates an AgentTask from a GitLabIssueEvent and integration configuration.
        /// </summary>
        /// <param name="issueEvent">The GitLab issue event from the webhook</param>
        /// <param name="config">The integration configuration containing target repository information</param>
        /// <returns>A populated AgentTask instance</returns>
        public static AgentTask FromGitLabEvent(GitLabIssueEvent issueEvent, GitLabIntegrationConfig config)
        {
            if (issueEvent == null)
                throw new ArgumentNullException(nameof(issueEvent));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.TargetRepoUrl))
                throw new ArgumentException("Target repository URL must be provided in configuration", nameof(config));

            var attrs = issueEvent.ObjectAttributes;
            var project = issueEvent.Project;

            // Extract labels from the event
            var labels = new List<string>();
            if (attrs?.Labels != null && attrs.Labels.Length > 0)
            {
                labels.AddRange(attrs.Labels.Select(l => l.Title).Where(t => !string.IsNullOrWhiteSpace(t)));
            }
            else if (issueEvent.Labels != null && issueEvent.Labels.Length > 0)
            {
                labels.AddRange(issueEvent.Labels.Select(l => l.Title).Where(t => !string.IsNullOrWhiteSpace(t)));
            }

            // Note: GitLab doesn't provide assignee usernames directly in webhook payload
            // The ObjectAttributes.AssigneeIds array contains numeric IDs, not usernames
            // For now, we'll leave assignees empty as the webhook doesn't include this info
            var assignees = new List<string>();

            return new AgentTask
            {
                Source = "gitlab",
                InstanceUrl = config.BaseUrl ?? string.Empty,
                SourceProjectId = project?.Id ?? 0,
                SourceProjectPath = project?.PathWithNamespace ?? string.Empty,
                IssueIid = attrs?.Iid ?? 0,
                Title = attrs?.Title ?? string.Empty,
                Description = attrs?.Description ?? string.Empty,
                Labels = labels,
                Author = issueEvent.User?.Username ?? string.Empty,
                Assignees = assignees,
                State = attrs?.State ?? string.Empty,
                WebUrl = attrs?.Url ?? string.Empty,
                EventAction = attrs?.Action ?? string.Empty,
                EventTimestamp = attrs?.UpdatedAt ?? DateTime.UtcNow,
                TargetRepoUrl = config.TargetRepoUrl,
                TargetRepoRef = config.TargetRepoRef ?? string.Empty
            };
        }

        /// <summary>
        /// Generates a deterministic deduplication key for idempotency checks.
        /// The key is based on (source_project_id, issue_iid, action, timestamp).
        /// </summary>
        /// <returns>A deduplication key string</returns>
        public string GetDeduplicationKey()
        {
            // Use UTC ticks to ensure consistent timestamp representation
            var timestampTicks = EventTimestamp.ToUniversalTime().Ticks;
            return $"{SourceProjectId}:{IssueIid}:{EventAction}:{timestampTicks}";
        }
    }
}
