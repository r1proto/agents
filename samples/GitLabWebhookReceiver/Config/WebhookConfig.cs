using System.Configuration;

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
    }
}
