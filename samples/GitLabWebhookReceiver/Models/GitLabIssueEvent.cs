using System;
using Newtonsoft.Json;

namespace GitLabWebhookReceiver.Models
{
    /// <summary>
    /// Represents a GitLab issue webhook event payload.
    /// Based on GitLab webhook documentation for issue events.
    /// </summary>
    public class GitLabIssueEvent
    {
        /// <summary>Event type (e.g., "issue")</summary>
        [JsonProperty("object_kind")]
        public string ObjectKind { get; set; }

        /// <summary>User who triggered the event</summary>
        [JsonProperty("user")]
        public GitLabUser User { get; set; }

        /// <summary>Project information</summary>
        [JsonProperty("project")]
        public GitLabProject Project { get; set; }

        /// <summary>Issue attributes and changes</summary>
        [JsonProperty("object_attributes")]
        public GitLabIssueAttributes ObjectAttributes { get; set; }

        /// <summary>Labels associated with the issue</summary>
        [JsonProperty("labels")]
        public GitLabLabel[] Labels { get; set; }

        /// <summary>Changes made to the issue (for update events)</summary>
        [JsonProperty("changes")]
        public GitLabChanges Changes { get; set; }

        /// <summary>Repository information</summary>
        [JsonProperty("repository")]
        public GitLabRepository Repository { get; set; }
    }

    /// <summary>
    /// Represents a GitLab user in webhook payloads.
    /// </summary>
    public class GitLabUser
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }
    }

    /// <summary>
    /// Represents a GitLab project in webhook payloads.
    /// </summary>
    public class GitLabProject
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("web_url")]
        public string WebUrl { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        [JsonProperty("path_with_namespace")]
        public string PathWithNamespace { get; set; }
    }

    /// <summary>
    /// Represents issue attributes in GitLab webhook payloads.
    /// </summary>
    public class GitLabIssueAttributes
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("iid")]
        public int Iid { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("author_id")]
        public int AuthorId { get; set; }

        [JsonProperty("assignee_ids")]
        public int[] AssigneeIds { get; set; }

        [JsonProperty("milestone_id")]
        public int? MilestoneId { get; set; }

        [JsonProperty("project_id")]
        public int ProjectId { get; set; }

        [JsonProperty("labels")]
        public GitLabLabel[] Labels { get; set; }
    }

    /// <summary>
    /// Represents a GitLab label.
    /// </summary>
    public class GitLabLabel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents changes made to an issue (for update events).
    /// </summary>
    public class GitLabChanges
    {
        [JsonProperty("title")]
        public GitLabChangeInfo Title { get; set; }

        [JsonProperty("description")]
        public GitLabChangeInfo Description { get; set; }

        [JsonProperty("labels")]
        public GitLabLabelsChange Labels { get; set; }

        [JsonProperty("updated_at")]
        public GitLabChangeInfo UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents a change to a simple field.
    /// </summary>
    public class GitLabChangeInfo
    {
        [JsonProperty("previous")]
        public string Previous { get; set; }

        [JsonProperty("current")]
        public string Current { get; set; }
    }

    /// <summary>
    /// Represents a change to labels.
    /// </summary>
    public class GitLabLabelsChange
    {
        [JsonProperty("previous")]
        public GitLabLabel[] Previous { get; set; }

        [JsonProperty("current")]
        public GitLabLabel[] Current { get; set; }
    }

    /// <summary>
    /// Represents repository information in GitLab webhook payloads.
    /// </summary>
    public class GitLabRepository
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("homepage")]
        public string Homepage { get; set; }
    }
}
