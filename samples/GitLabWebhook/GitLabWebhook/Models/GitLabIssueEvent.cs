using System.Text.Json.Serialization;

namespace GitLabWebhook.Models
{
    /// <summary>
    /// Represents the top-level payload of a GitLab issue webhook event.
    /// See: https://docs.gitlab.com/ee/user/project/integrations/webhook_events.html#issue-events
    /// </summary>
    public sealed class GitLabIssueEvent
    {
        /// <summary>Always "issue" for issue events.</summary>
        [JsonPropertyName("object_kind")]
        public string ObjectKind { get; set; } = string.Empty;

        /// <summary>The user who triggered the event.</summary>
        [JsonPropertyName("user")]
        public GitLabUser? User { get; set; }

        /// <summary>The project the issue belongs to.</summary>
        [JsonPropertyName("project")]
        public GitLabProject? Project { get; set; }

        /// <summary>Issue-specific attributes, including the action that triggered the event.</summary>
        [JsonPropertyName("object_attributes")]
        public GitLabIssueObjectAttributes? ObjectAttributes { get; set; }
    }

    /// <summary>Represents the GitLab user who triggered the event.</summary>
    public sealed class GitLabUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>Represents the GitLab project the issue belongs to.</summary>
    public sealed class GitLabProject
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("web_url")]
        public string WebUrl { get; set; } = string.Empty;
    }

    /// <summary>Represents the issue-specific attributes within the webhook payload.</summary>
    public sealed class GitLabIssueObjectAttributes
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("iid")]
        public int Iid { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// The action that triggered this event.
        /// Known values: "open", "close", "reopen", "update".
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;
    }
}
