using System;
using GitLabWebhookReceiver.Config;
using GitLabWebhookReceiver.Dispatcher;

namespace GitLabWebhookReceiver.WebhookReceiver
{
    /// <summary>
    /// Entry point for the GitLab webhook receiver service.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== GitLab Issue Webhook Receiver ===");
            Console.WriteLine();

            // Load configuration
            var host = WebhookConfig.Host;
            var port = WebhookConfig.Port;
            var secret = WebhookConfig.WebhookSecret;

            if (string.IsNullOrEmpty(secret))
            {
                Console.Error.WriteLine("ERROR: GitLab webhook secret is not configured.");
                Console.Error.WriteLine("Please set the 'GitLab:WebhookSecret' key in App.config.");
                Console.WriteLine();
                Console.WriteLine("Press ENTER to exit.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine($"Host: {host}");
            Console.WriteLine($"Port: {port}");
            Console.WriteLine($"Webhook secret: {MaskSecret(secret)}");
            Console.WriteLine();

            // Validate integration configuration
            var validationError = WebhookConfig.ValidateConfig();
            if (validationError != null)
            {
                Console.Error.WriteLine($"WARNING: Configuration validation failed: {validationError}");
                Console.Error.WriteLine("Using stub dispatcher. To enable full dispatcher, configure:");
                Console.Error.WriteLine("  - GitLab:BaseUrl (https:// URL)");
                Console.Error.WriteLine("  - GitLab:TargetRepoUrl (target repository URL)");
                Console.WriteLine();
            }

            // Create dispatcher based on configuration
            IIssueEventDispatcher dispatcher;
            if (validationError == null && WebhookConfig.Enabled)
            {
                // Use real dispatcher with agent submission
                Console.WriteLine("Using GitLab dispatcher with agent submission");
                Console.WriteLine($"GitLab instance: {WebhookConfig.DisplayName} ({WebhookConfig.GitLabBaseUrl})");
                Console.WriteLine($"Target repo: {WebhookConfig.TargetRepoUrl}");
                if (!string.IsNullOrEmpty(WebhookConfig.TargetRepoRef))
                {
                    Console.WriteLine($"Target ref: {WebhookConfig.TargetRepoRef}");
                }
                Console.WriteLine();

                var agentSubmissionService = new StubAgentSubmissionService();
                dispatcher = new GitLabIssueEventDispatcher(
                    agentSubmissionService,
                    WebhookConfig.GitLabBaseUrl,
                    WebhookConfig.TargetRepoUrl,
                    WebhookConfig.TargetRepoRef);
            }
            else
            {
                // Use stub dispatcher (logs to console only)
                Console.WriteLine("Using stub dispatcher (no agent submission)");
                Console.WriteLine();
                dispatcher = new StubIssueEventDispatcher();
            }

            // Create and start the webhook server
            var server = new WebhookServer(host, port, dispatcher, secret);

            // Handle graceful shutdown on Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine();
                Console.WriteLine("[Program] Shutting down...");
                server.Stop();
            };

            try
            {
                server.Start();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Program] Fatal error: {ex.Message}");
                Console.Error.WriteLine($"[Program] Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Masks a secret string for display purposes.
        /// </summary>
        private static string MaskSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return "(empty)";

            if (secret.Length <= 4)
                return new string('*', secret.Length);

            return secret.Substring(0, 2) + new string('*', secret.Length - 4) + secret.Substring(secret.Length - 2);
        }
    }
}
