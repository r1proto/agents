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

            // Create dispatcher (using stub implementation for now)
            var dispatcher = new StubIssueEventDispatcher();

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
