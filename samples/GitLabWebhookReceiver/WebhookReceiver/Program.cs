using System;
using GitLabWebhookReceiver.Config;
using GitLabWebhookReceiver.Dispatcher;
using GitLabWebhookReceiver.Submission;

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

            // Load integration configuration
            var integrationConfig = WebhookConfig.GetIntegrationConfig();

            // Validate integration configuration
            try
            {
                integrationConfig.Validate();
                Console.WriteLine("[Program] GitLab integration configuration loaded:");
                Console.WriteLine($"  Base URL: {integrationConfig.BaseUrl}");
                Console.WriteLine($"  Target Repo: {integrationConfig.TargetRepoUrl}");
                Console.WriteLine($"  Display Name: {integrationConfig.DisplayName}");
                Console.WriteLine($"  Enabled: {integrationConfig.Enabled}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Program] ERROR: Invalid GitLab integration configuration: {ex.Message}");
                Console.Error.WriteLine("[Program] The service will use the stub dispatcher (no agent submission).");
                Console.WriteLine();
            }

            // Create dispatcher based on configuration
            IIssueEventDispatcher dispatcher;

            if (integrationConfig.Enabled)
            {
                try
                {
                    // Load OpenCode configuration
                    var openCodeExe = WebhookConfig.OpenCodeExecutable;
                    var agentName = WebhookConfig.OpenCodeAgentName;
                    var dedupWindowMinutes = WebhookConfig.DeduplicationWindowMinutes;

                    Console.WriteLine("[Program] OpenCode agent configuration:");
                    Console.WriteLine($"  Executable: {openCodeExe}");
                    Console.WriteLine($"  Agent Name: {agentName}");
                    Console.WriteLine($"  Deduplication Window: {dedupWindowMinutes} minutes");
                    Console.WriteLine();

                    // Create agent submission service
                    var submissionService = new OpenCodeAgentSubmissionService(
                        openCodeExe,
                        agentName,
                        dedupWindowMinutes);

                    // Create production dispatcher with agent submission
                    dispatcher = new AgentIssueEventDispatcher(submissionService, integrationConfig);

                    Console.WriteLine("[Program] Using AgentIssueEventDispatcher with OpenCode integration");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Program] ERROR: Failed to initialize agent dispatcher: {ex.Message}");
                    Console.Error.WriteLine("[Program] Falling back to stub dispatcher.");
                    dispatcher = new StubIssueEventDispatcher();
                }
            }
            else
            {
                Console.WriteLine("[Program] GitLab integration is disabled - using stub dispatcher");
                dispatcher = new StubIssueEventDispatcher();
            }

            Console.WriteLine();

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
