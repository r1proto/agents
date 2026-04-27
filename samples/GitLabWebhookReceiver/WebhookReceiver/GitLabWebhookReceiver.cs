using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using GitLabWebhookReceiver.Models;
using GitLabWebhookReceiver.Dispatcher;
using GitLabWebhookReceiver.Config;

namespace GitLabWebhookReceiver.WebhookReceiver
{
    /// <summary>
    /// Lightweight, stateless HTTP endpoint that receives GitLab issue webhook POST requests.
    /// Validates the webhook secret, parses the JSON payload, and forwards valid issue events
    /// to the dispatcher layer.
    /// </summary>
    public class GitLabWebhookReceiver
    {
        private readonly IIssueEventDispatcher _dispatcher;
        private readonly string _webhookSecret;

        /// <summary>
        /// Initializes a new instance of the GitLabWebhookReceiver.
        /// </summary>
        /// <param name="dispatcher">The dispatcher to forward validated events to</param>
        /// <param name="webhookSecret">The secret token to validate incoming requests</param>
        public GitLabWebhookReceiver(IIssueEventDispatcher dispatcher, string webhookSecret)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _webhookSecret = webhookSecret ?? throw new ArgumentNullException(nameof(webhookSecret));
        }

        /// <summary>
        /// Handles an incoming HTTP webhook request.
        /// Returns an HTTP response with appropriate status code and message.
        /// </summary>
        /// <param name="request">The HTTP request to process</param>
        /// <returns>A WebhookResponse containing status code and message</returns>
        public WebhookResponse HandleRequest(HttpListenerRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Only accept POST requests
            if (request.HttpMethod != "POST")
            {
                return new WebhookResponse
                {
                    StatusCode = 405, // Method Not Allowed
                    Message = "Only POST requests are accepted."
                };
            }

            // Validate X-Gitlab-Token header
            var gitlabToken = request.Headers["X-Gitlab-Token"];
            if (string.IsNullOrEmpty(gitlabToken))
            {
                Console.Error.WriteLine("[WebhookReceiver] Missing X-Gitlab-Token header.");
                return new WebhookResponse
                {
                    StatusCode = 401, // Unauthorized
                    Message = "Missing X-Gitlab-Token header."
                };
            }

            if (gitlabToken != _webhookSecret)
            {
                Console.Error.WriteLine("[WebhookReceiver] Invalid X-Gitlab-Token.");
                return new WebhookResponse
                {
                    StatusCode = 401, // Unauthorized
                    Message = "Invalid X-Gitlab-Token."
                };
            }

            // Read and parse the request body
            string requestBody;
            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WebhookReceiver] Error reading request body: {ex.Message}");
                return new WebhookResponse
                {
                    StatusCode = 400, // Bad Request
                    Message = "Error reading request body."
                };
            }

            // Parse JSON payload
            GitLabIssueEvent issueEvent;
            try
            {
                issueEvent = JsonConvert.DeserializeObject<GitLabIssueEvent>(requestBody);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[WebhookReceiver] Malformed JSON payload: {ex.Message}");
                return new WebhookResponse
                {
                    StatusCode = 400, // Bad Request
                    Message = "Malformed JSON payload."
                };
            }

            if (issueEvent == null)
            {
                Console.Error.WriteLine("[WebhookReceiver] Deserialized payload is null.");
                return new WebhookResponse
                {
                    StatusCode = 400, // Bad Request
                    Message = "Invalid payload."
                };
            }

            // Check X-Gitlab-Event header to determine event type
            var gitlabEvent = request.Headers["X-Gitlab-Event"];

            // Only process "Issue Hook" events
            if (gitlabEvent != "Issue Hook")
            {
                Console.WriteLine($"[WebhookReceiver] Ignoring unsupported event type: {gitlabEvent ?? "unknown"}");
                return new WebhookResponse
                {
                    StatusCode = 200, // OK - silently ignore
                    Message = "Event type not supported."
                };
            }

            // Check the action - only forward "open" or "update" actions
            var action = issueEvent.ObjectAttributes?.Action;
            if (action != "open" && action != "update")
            {
                Console.WriteLine($"[WebhookReceiver] Ignoring issue event with action: {action ?? "unknown"}");
                return new WebhookResponse
                {
                    StatusCode = 200, // OK - silently ignore
                    Message = "Action not supported."
                };
            }

            // Forward to dispatcher
            try
            {
                _dispatcher.DispatchIssueEvent(issueEvent);
                Console.WriteLine($"[WebhookReceiver] Successfully dispatched issue event (action: {action}).");
                return new WebhookResponse
                {
                    StatusCode = 200, // OK
                    Message = "Event processed successfully."
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WebhookReceiver] Error dispatching event: {ex.Message}");
                return new WebhookResponse
                {
                    StatusCode = 500, // Internal Server Error
                    Message = "Error processing event."
                };
            }
        }
    }

    /// <summary>
    /// Represents an HTTP response from the webhook receiver.
    /// </summary>
    public class WebhookResponse
    {
        /// <summary>HTTP status code</summary>
        public int StatusCode { get; set; }

        /// <summary>Response message</summary>
        public string Message { get; set; }
    }
}
