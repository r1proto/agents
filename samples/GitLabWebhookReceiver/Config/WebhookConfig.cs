using System;
using System.Configuration;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Config
{
    /// <summary>
    /// Configuration settings for the GitLab webhook receiver and integration.
    /// Follows the pattern from RabbitMqOrderService/Infrastructure/AppConfig.cs
    /// </summary>
    public static class WebhookConfig
    {
        /// <summary>
        /// The secret token used to validate incoming GitLab webhook requests.
        /// This should match the token configured in GitLab webhook settings.
        /// </summary>
        public static string WebhookSecret =>
            ConfigurationManager.AppSettings["GitLab:WebhookSecret"] ?? string.Empty;

        /// <summary>
        /// The port on which the webhook receiver HTTP server listens.
        /// Default: 8080
        /// </summary>
        public static int Port
        {
            get
            {
                var raw = ConfigurationManager.AppSettings["Webhook:Port"];
                return int.TryParse(raw, out var port) ? port : 8080;
            }
        }

        /// <summary>
        /// The host address on which the webhook receiver HTTP server listens.
        /// Default: localhost
        /// </summary>
        public static string Host =>
            ConfigurationManager.AppSettings["Webhook:Host"] ?? "localhost";

        // GitLab Integration Configuration (Issue #5)

        /// <summary>
        /// Base URL of the GitLab instance (e.g. https://gitlab.company.internal).
        /// Trailing slashes are normalized away.
        /// </summary>
        public static string GitLabBaseUrl
        {
            get
            {
                var url = ConfigurationManager.AppSettings["GitLab:BaseUrl"] ?? string.Empty;
                return url.TrimEnd('/');
            }
        }

        /// <summary>
        /// URL of the target repository the OpenCode AI Agent should clone and work in
        /// when resolving issues (e.g. https://github.com/org/backend).
        /// This is distinct from the GitLab project where issues are filed.
        /// </summary>
        public static string TargetRepoUrl =>
            ConfigurationManager.AppSettings["GitLab:TargetRepoUrl"] ?? string.Empty;

        /// <summary>
        /// Optional default branch/ref for the agent to start from.
        /// Defaults to empty string, which means use the repository's default branch.
        /// </summary>
        public static string TargetRepoRef =>
            ConfigurationManager.AppSettings["GitLab:TargetRepoRef"] ?? string.Empty;

        /// <summary>
        /// Boolean flag to enable or disable the GitLab integration.
        /// Defaults to false (opt-in).
        /// </summary>
        public static bool Enabled
        {
            get
            {
                var raw = ConfigurationManager.AppSettings["GitLab:Enabled"];
                return bool.TryParse(raw, out var enabled) && enabled;
            }
        }

        /// <summary>
        /// Optional human-readable label for the GitLab instance.
        /// </summary>
        public static string DisplayName =>
            ConfigurationManager.AppSettings["GitLab:DisplayName"] ?? "GitLab";

        /// <summary>
        /// Validates the integration configuration and returns an error message if invalid.
        /// Returns null if configuration is valid.
        /// </summary>
        public static string ValidateConfig()
        {
            if (string.IsNullOrEmpty(WebhookSecret))
                return "GitLab webhook secret is not configured (GitLab:WebhookSecret)";

            if (string.IsNullOrEmpty(GitLabBaseUrl))
                return "GitLab base URL is not configured (GitLab:BaseUrl)";

            if (!GitLabBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return "GitLab base URL must be an https:// URL";

            if (string.IsNullOrEmpty(TargetRepoUrl))
                return "Target repository URL is not configured (GitLab:TargetRepoUrl)";

            if (!Uri.TryCreate(TargetRepoUrl, UriKind.Absolute, out _))
                return "Target repository URL is not a valid URL";

            return null;
        }
    }
}
