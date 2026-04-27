namespace GitLabWebhook.Models
{
    /// <summary>
    /// Internal model for GitLab issue webhook events.
    /// Captures the minimum fields needed for dispatch from a GitLab issue webhook payload.
    /// </summary>
    public class GitLabIssueWebhookEvent
    {
        /// <summary>
        /// Project ID from the GitLab webhook payload.
        /// Source: project.id
        /// Required field.
        /// </summary>
        public long ProjectId { get; set; }

        /// <summary>
        /// Project path with namespace (e.g., "group/project").
        /// Source: project.path_with_namespace
        /// Required field.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Issue internal ID (IID) within the project.
        /// Source: object_attributes.iid
        /// Required field.
        /// </summary>
        public long IssueIid { get; set; }

        /// <summary>
        /// Issue title.
        /// Source: object_attributes.title
        /// Required field.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Issue description/body.
        /// Source: object_attributes.description
        /// Optional field. Empty string if not provided.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of label titles attached to the issue.
        /// Source: labels[].title
        /// Optional field. Empty list if not provided.
        /// </summary>
        public List<string> Labels { get; set; }

        /// <summary>
        /// Username of the issue author.
        /// Source: user.username
        /// Required field.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// List of usernames assigned to the issue.
        /// Source: assignees[].username
        /// Optional field. Empty list if not provided.
        /// </summary>
        public List<string> Assignees { get; set; }

        /// <summary>
        /// Issue state (e.g., "opened", "closed", "reopened").
        /// Source: object_attributes.state
        /// Required field.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Web URL to view the issue.
        /// Source: object_attributes.url
        /// Required field.
        /// </summary>
        public string WebUrl { get; set; }

        /// <summary>
        /// Action/event type (e.g., "open", "close", "reopen", "update").
        /// Source: object_attributes.action
        /// Required field.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Timestamp of the event.
        /// Source: object_attributes.created_at or object_attributes.updated_at
        /// Required field.
        /// </summary>
        public DateTime Timestamp { get; set; }

        public GitLabIssueWebhookEvent()
        {
            ProjectPath = string.Empty;
            Title = string.Empty;
            Description = string.Empty;
            Labels = new List<string>();
            Author = string.Empty;
            Assignees = new List<string>();
            State = string.Empty;
            WebUrl = string.Empty;
            Action = string.Empty;
        }
    }
}
