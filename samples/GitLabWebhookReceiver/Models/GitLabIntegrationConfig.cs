using System;

namespace GitLabWebhookReceiver.Models
{
    /// <summary>
    /// Represents IT-managed GitLab integration configuration.
    /// This configuration is admin-managed only and not user-facing.
    /// </summary>
    public class GitLabIntegrationConfig
    {
        /// <summary>
        /// The GitLab instance base URL (e.g. "https://gitlab.company.internal").
        /// Must be HTTPS. Trailing slashes are normalized.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// The shared secret used to validate incoming webhook requests.
        /// This value should be stored securely and not logged.
        /// </summary>
        public string WebhookSecret { get; set; }

        /// <summary>
        /// The URL of the codebase repository the OpenCode AI Agent should clone and work in
        /// when resolving issues (e.g. "https://github.com/org/backend").
        /// This is distinct from the GitLab project where issues are filed.
        /// REQUIRED field.
        /// </summary>
        public string TargetRepoUrl { get; set; }

        /// <summary>
        /// Optional default branch/ref for the agent to start from.
        /// Defaults to the repository's default branch when empty.
        /// </summary>
        public string TargetRepoRef { get; set; }

        /// <summary>
        /// Boolean flag to enable or disable the integration without removing config.
        /// Defaults to false (opt-in).
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Optional human-readable label for the GitLab instance.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Default constructor. Initializes with safe defaults.
        /// </summary>
        public GitLabIntegrationConfig()
        {
            Enabled = false;
            TargetRepoRef = string.Empty;
            DisplayName = string.Empty;
        }

        /// <summary>
        /// Validates the configuration and normalizes field values.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
        public void Validate()
        {
            // Validate base URL
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new ArgumentException("BaseUrl is required and cannot be empty");

            // Normalize trailing slashes
            BaseUrl = BaseUrl.TrimEnd('/');

            // Validate HTTPS requirement
            if (!BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("BaseUrl must use HTTPS protocol");

            // Validate URL format
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri))
                throw new ArgumentException("BaseUrl must be a valid absolute URL");

            // Validate webhook secret
            if (string.IsNullOrWhiteSpace(WebhookSecret))
                throw new ArgumentException("WebhookSecret is required and cannot be empty");

            // Validate target repository URL
            if (string.IsNullOrWhiteSpace(TargetRepoUrl))
                throw new ArgumentException("TargetRepoUrl is required and cannot be empty");

            if (!Uri.TryCreate(TargetRepoUrl, UriKind.Absolute, out var repoUri))
                throw new ArgumentException("TargetRepoUrl must be a valid absolute URL");
        }

        /// <summary>
        /// Creates a sanitized copy of the configuration for logging or serialization.
        /// The webhook secret is masked to prevent exposure.
        /// </summary>
        /// <returns>A configuration instance with the secret masked</returns>
        public GitLabIntegrationConfig ToSanitized()
        {
            return new GitLabIntegrationConfig
            {
                BaseUrl = this.BaseUrl,
                WebhookSecret = string.IsNullOrWhiteSpace(this.WebhookSecret) ? string.Empty : "***REDACTED***",
                TargetRepoUrl = this.TargetRepoUrl,
                TargetRepoRef = this.TargetRepoRef,
                Enabled = this.Enabled,
                DisplayName = this.DisplayName
            };
        }
    }
}
