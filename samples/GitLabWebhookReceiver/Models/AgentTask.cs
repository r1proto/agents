using System;

namespace GitLabWebhookReceiver.Models
{
    /// <summary>
    /// Internal task schema for dispatching GitLab issue events to the OpenCode AI Agent.
    /// This schema is provider-agnostic and separates the source project (where the issue
    /// is filed) from the target project (where code changes will be made).
    /// </summary>
    public class AgentTask
    {
        // Source — GitLab issue metadata

        /// <summary>Fixed value "gitlab" indicating the source provider</summary>
        public string Source { get; set; }

        /// <summary>Base URL of the GitLab instance</summary>
        public string InstanceUrl { get; set; }

        /// <summary>Numeric ID of the GitLab project where the issue is filed</summary>
        public int SourceProjectId { get; set; }

        /// <summary>Full path of the GitLab project (e.g. group/subgroup/tracker)</summary>
        public string SourceProjectPath { get; set; }

        /// <summary>Project-scoped issue identifier</summary>
        public int IssueIid { get; set; }

        /// <summary>Issue title</summary>
        public string Title { get; set; }

        /// <summary>Issue body/description</summary>
        public string Description { get; set; }

        /// <summary>List of label names</summary>
        public string[] Labels { get; set; }

        /// <summary>Issue author username</summary>
        public string Author { get; set; }

        /// <summary>List of assignee usernames</summary>
        public string[] Assignees { get; set; }

        /// <summary>Issue state (opened, closed, etc.)</summary>
        public string State { get; set; }

        /// <summary>Human-visible URL of the GitLab issue</summary>
        public string WebUrl { get; set; }

        /// <summary>Webhook action that triggered dispatch (open, update, etc.)</summary>
        public string EventAction { get; set; }

        /// <summary>ISO-8601 timestamp of the triggering event</summary>
        public DateTime EventTimestamp { get; set; }

        // Target — codebase the agent should work on

        /// <summary>URL of the repository the agent should clone/check out and modify (required)</summary>
        public string TargetRepoUrl { get; set; }

        /// <summary>Branch or ref the agent should start from (optional; empty string means default branch)</summary>
        public string TargetRepoRef { get; set; }

        /// <summary>
        /// Factory method to create an AgentTask from a GitLabIssueEvent and integration configuration.
        /// </summary>
        /// <param name="issueEvent">The GitLab issue event</param>
        /// <param name="instanceUrl">Base URL of the GitLab instance</param>
        /// <param name="targetRepoUrl">URL of the target repository for code changes</param>
        /// <param name="targetRepoRef">Optional target branch/ref (defaults to empty string)</param>
        /// <returns>A populated AgentTask instance</returns>
        public static AgentTask FromGitLabIssueEvent(
            GitLabIssueEvent issueEvent,
            string instanceUrl,
            string targetRepoUrl,
            string targetRepoRef = "")
        {
            if (issueEvent == null)
                throw new ArgumentNullException(nameof(issueEvent));
            if (string.IsNullOrEmpty(targetRepoUrl))
                throw new ArgumentException("Target repository URL is required", nameof(targetRepoUrl));

            var attrs = issueEvent.ObjectAttributes ?? throw new ArgumentException("ObjectAttributes is required", nameof(issueEvent));
            var project = issueEvent.Project ?? throw new ArgumentException("Project is required", nameof(issueEvent));
            var user = issueEvent.User;

            // Extract labels
            var labels = issueEvent.Labels ?? new GitLabLabel[0];
            var labelNames = new string[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labelNames[i] = labels[i]?.Title ?? "";
            }

            // Extract assignees - GitLab webhook doesn't provide assignee usernames directly,
            // so we'll use empty array for now (can be enhanced later if needed)
            var assignees = new string[0];

            return new AgentTask
            {
                // Source fields
                Source = "gitlab",
                InstanceUrl = instanceUrl ?? "",
                SourceProjectId = project.Id,
                SourceProjectPath = project.PathWithNamespace ?? "",
                IssueIid = attrs.Iid,
                Title = attrs.Title ?? "",
                Description = attrs.Description ?? "",
                Labels = labelNames,
                Author = user?.Username ?? "",
                Assignees = assignees,
                State = attrs.State ?? "",
                WebUrl = attrs.Url ?? "",
                EventAction = attrs.Action ?? "",
                EventTimestamp = attrs.UpdatedAt,

                // Target fields
                TargetRepoUrl = targetRepoUrl,
                TargetRepoRef = targetRepoRef ?? ""
            };
        }
    }
}
