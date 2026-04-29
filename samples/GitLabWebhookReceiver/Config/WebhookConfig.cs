using System;
using System.Configuration;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Config
{
    /// <summary>
    /// Configuration settings for the GitLab webhook receiver.
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

        /// <summary>
        /// Loads the GitLab integration configuration from app settings.
        /// </summary>
        /// <returns>A GitLabIntegrationConfig instance</returns>
        public static GitLabIntegrationConfig GetIntegrationConfig()
        {
            var config = new GitLabIntegrationConfig
            {
                BaseUrl = ConfigurationManager.AppSettings["GitLab:BaseUrl"] ?? string.Empty,
                WebhookSecret = ConfigurationManager.AppSettings["GitLab:WebhookSecret"] ?? string.Empty,
                TargetRepoUrl = ConfigurationManager.AppSettings["GitLab:TargetRepoUrl"] ?? string.Empty,
                TargetRepoRef = ConfigurationManager.AppSettings["GitLab:TargetRepoRef"] ?? string.Empty,
                DisplayName = ConfigurationManager.AppSettings["GitLab:DisplayName"] ?? "GitLab",
                Enabled = ParseBool(ConfigurationManager.AppSettings["GitLab:Enabled"], false)
            };

            return config;
        }

        /// <summary>
        /// Gets the OpenCode executable path from configuration.
        /// Default: "opencode" (assumes it's in PATH)
        /// </summary>
        public static string OpenCodeExecutable =>
            ConfigurationManager.AppSettings["OpenCode:Executable"] ?? "opencode";

        /// <summary>
        /// Gets the OpenCode agent name to use for submissions.
        /// Default: "dotnet-feature-coder"
        /// </summary>
        public static string OpenCodeAgentName =>
            ConfigurationManager.AppSettings["OpenCode:AgentName"] ?? "dotnet-feature-coder";

        /// <summary>
        /// Gets the deduplication window in minutes.
        /// Default: 60 minutes
        /// </summary>
        public static int DeduplicationWindowMinutes
        {
            get
            {
                var raw = ConfigurationManager.AppSettings["OpenCode:DeduplicationWindowMinutes"];
                return int.TryParse(raw, out var minutes) ? minutes : 60;
            }
        }

        /// <summary>
        /// Helper method to parse boolean configuration values.
        /// </summary>
        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (bool.TryParse(value, out var result))
                return result;

            // Handle common boolean string representations
            value = value.ToLowerInvariant().Trim();
            if (value == "1" || value == "yes" || value == "on" || value == "true")
                return true;
            if (value == "0" || value == "no" || value == "off" || value == "false")
                return false;

            return defaultValue;
        }
    }
}
