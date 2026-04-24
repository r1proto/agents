using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitLabWebhook.Interfaces;
using GitLabWebhook.Models;
using Microsoft.AspNetCore.Mvc;

namespace GitLabWebhook.Controllers
{
    /// <summary>
    /// Receives GitLab issue webhook POST requests, validates the token,
    /// parses the payload, and forwards eligible events to the dispatcher.
    /// </summary>
    [ApiController]
    public sealed class GitLabWebhookController : ControllerBase
    {
        private const string GitLabTokenHeader = "X-Gitlab-Token";
        private const string GitLabEventHeader = "X-Gitlab-Event";
        private const string IssueHookEventType = "Issue Hook";

        private readonly IIssueEventDispatcher _dispatcher;
        private readonly string _webhookSecret;
        private readonly ILogger<GitLabWebhookController> _logger;

        public GitLabWebhookController(
            IIssueEventDispatcher dispatcher,
            IConfiguration configuration,
            ILogger<GitLabWebhookController> logger)
        {
            _dispatcher = dispatcher;
            _webhookSecret = configuration["GitLab:WebhookSecret"] ?? string.Empty;
            _logger = logger;
        }

        /// <summary>
        /// Accepts POST requests containing a GitLab issue webhook payload.
        /// </summary>
        /// <remarks>
        /// - Returns 401 if the <c>X-Gitlab-Token</c> header is missing or invalid.
        /// - Returns 400 if the request body cannot be parsed as a GitLab issue event.
        /// - Returns 200 and silently ignores payloads whose event type is not "Issue Hook"
        ///   or whose action is not "open" or "update".
        /// - Returns 200 and forwards the event to the dispatcher for "open" and "update" actions.
        /// </remarks>
        [HttpPost("/webhooks/gitlab/issues")]
        public async Task<IActionResult> ReceiveIssueWebhook(CancellationToken cancellationToken)
        {
            // 1. Validate the webhook secret token using constant-time comparison
            //    to prevent timing-based token enumeration attacks.
            if (!Request.Headers.TryGetValue(GitLabTokenHeader, out var tokenValues) ||
                !TokensEqual(tokenValues.ToString(), _webhookSecret))
            {
                return Unauthorized();
            }

            // 2. Parse the JSON body.
            GitLabIssueEvent? issueEvent;
            try
            {
                issueEvent = await JsonSerializer.DeserializeAsync<GitLabIssueEvent>(
                    Request.Body,
                    cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                return BadRequest();
            }

            if (issueEvent is null)
            {
                return BadRequest();
            }

            // 3. Check the event type header.
            var eventType = Request.Headers[GitLabEventHeader].ToString();
            if (!string.Equals(eventType, IssueHookEventType, StringComparison.OrdinalIgnoreCase))
            {
                // Unsupported / ignored event type — silently accept.
                return Ok();
            }

            // 4. Only forward "open" and "update" actions.
            var action = issueEvent.ObjectAttributes?.Action ?? string.Empty;
            if (!string.Equals(action, "open", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
            {
                // Other actions (close, reopen, …) — silently accept.
                return Ok();
            }

            // 5. Forward to the dispatcher.
            await _dispatcher.DispatchAsync(issueEvent, cancellationToken);

            return Ok();
        }

        /// <summary>
        /// Compares two tokens using a constant-time algorithm to mitigate timing attacks.
        /// </summary>
        private static bool TokensEqual(string incoming, string expected)
        {
            var incomingBytes = Encoding.UTF8.GetBytes(incoming);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            return CryptographicOperations.FixedTimeEquals(incomingBytes, expectedBytes);
        }
    }
}
