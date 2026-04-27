namespace GitLabWebhook.Parser
{
    /// <summary>
    /// Internal DTOs that mirror the GitLab webhook payload structure.
    /// These are used for deserialization only.
    /// </summary>
    internal class GitLabWebhookPayload
    {
        public string object_kind { get; set; }
        public GitLabProject project { get; set; }
        public GitLabObjectAttributes object_attributes { get; set; }
        public GitLabUser user { get; set; }
        public List<GitLabLabel> labels { get; set; }
        public List<GitLabAssignee> assignees { get; set; }
    }

    internal class GitLabProject
    {
        public long id { get; set; }
        public string path_with_namespace { get; set; }
    }

    internal class GitLabObjectAttributes
    {
        public long iid { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string state { get; set; }
        public string url { get; set; }
        public string action { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }

    internal class GitLabUser
    {
        public string username { get; set; }
    }

    internal class GitLabLabel
    {
        public string title { get; set; }
    }

    internal class GitLabAssignee
    {
        public string username { get; set; }
    }
}
