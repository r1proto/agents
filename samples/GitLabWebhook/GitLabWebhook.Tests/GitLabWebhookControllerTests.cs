using System.Net;
using System.Net.Http.Json;
using System.Text;
using GitLabWebhook.Interfaces;
using GitLabWebhook.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GitLabWebhook.Tests
{
    /// <summary>
    /// Integration tests for <see cref="GitLabWebhook.Controllers.GitLabWebhookController"/>.
    /// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> to host the application in-process.
    /// </summary>
    [TestClass]
    public sealed class GitLabWebhookControllerTests
    {
        private const string WebhookPath = "/webhooks/gitlab/issues";
        private const string ValidToken = "test-secret-token";
        private const string GitLabTokenHeader = "X-Gitlab-Token";
        private const string GitLabEventHeader = "X-Gitlab-Event";
        private const string IssueHookEventType = "Issue Hook";

        // ── helpers ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an <see cref="HttpClient"/> backed by an in-process test server.
        /// The provided dispatcher spy is registered in place of the default dispatcher.
        /// </summary>
        private static HttpClient CreateClient(DispatcherSpy spy)
        {
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host =>
                {
                    host.UseSetting("GitLab:WebhookSecret", ValidToken);
                    host.ConfigureServices(services =>
                    {
                        services.RemoveAll<IIssueEventDispatcher>();
                        services.AddSingleton<IIssueEventDispatcher>(spy);
                    });
                });

            return factory.CreateClient();
        }

        private static StringContent IssuePayload(string action) =>
            new StringContent(
                $$"""
                {
                  "object_kind": "issue",
                  "user": { "id": 1, "name": "Alice", "username": "alice" },
                  "project": { "id": 10, "name": "my-project", "web_url": "https://gitlab.example.com/my-project" },
                  "object_attributes": {
                    "id": 100,
                    "iid": 5,
                    "title": "Test issue",
                    "state": "opened",
                    "action": "{{action}}"
                  }
                }
                """,
                Encoding.UTF8,
                "application/json");

        // ── valid token + issue event ─────────────────────────────────────────────────

        /// <summary>
        /// A POST with a valid token and an "open" action must return 200 and invoke the dispatcher.
        /// </summary>
        [TestMethod]
        public async Task Post_ValidTokenAndOpenAction_Returns200AndDispatchesCalled()
        {
            var spy = new DispatcherSpy();
            using var client = CreateClient(spy);

            var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
            {
                Content = IssuePayload("open")
            };
            request.Headers.Add(GitLabTokenHeader, ValidToken);
            request.Headers.Add(GitLabEventHeader, IssueHookEventType);

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, spy.CallCount, "Dispatcher should have been called exactly once.");
            Assert.AreEqual("open", spy.LastEvent?.ObjectAttributes?.Action);
        }

        /// <summary>
        /// A POST with a valid token and an "update" action must return 200 and invoke the dispatcher.
        /// </summary>
        [TestMethod]
        public async Task Post_ValidTokenAndUpdateAction_Returns200AndDispatchesCalled()
        {
            var spy = new DispatcherSpy();
            using var client = CreateClient(spy);

            var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
            {
                Content = IssuePayload("update")
            };
            request.Headers.Add(GitLabTokenHeader, ValidToken);
            request.Headers.Add(GitLabEventHeader, IssueHookEventType);

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, spy.CallCount);
            Assert.AreEqual("update", spy.LastEvent?.ObjectAttributes?.Action);
        }

        // ── invalid / missing token ───────────────────────────────────────────────────

        /// <summary>
        /// A POST with an invalid token must return 401 and must not invoke the dispatcher.
        /// </summary>
        [TestMethod]
        public async Task Post_InvalidToken_Returns401WithoutDispatching()
        {
            var spy = new DispatcherSpy();
            using var client = CreateClient(spy);

            var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
            {
                Content = IssuePayload("open")
            };
            request.Headers.Add(GitLabTokenHeader, "wrong-token");
            request.Headers.Add(GitLabEventHeader, IssueHookEventType);

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual(0, spy.CallCount, "Dispatcher must not be called for invalid tokens.");
        }

        /// <summary>
        /// A POST with no token header must return 401 and must not invoke the dispatcher.
        /// </summary>
        [TestMethod]
        public async Task Post_MissingToken_Returns401WithoutDispatching()
        {
            var spy = new DispatcherSpy();
            using var client = CreateClient(spy);

            var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
            {
                Content = IssuePayload("open")
            };
            request.Headers.Add(GitLabEventHeader, IssueHookEventType);
            // Deliberately omit GitLabTokenHeader.

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual(0, spy.CallCount, "Dispatcher must not be called when token header is absent.");
        }

        // ── malformed JSON ────────────────────────────────────────────────────────────

        /// <summary>
        /// A POST with a malformed JSON body must return 400 Bad Request.
        /// </summary>
        [TestMethod]
        public async Task Post_MalformedJson_Returns400()
        {
            var spy = new DispatcherSpy();
            using var client = CreateClient(spy);

            var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
            {
                Content = new StringContent("{ not valid json !!!}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add(GitLabTokenHeader, ValidToken);
            request.Headers.Add(GitLabEventHeader, IssueHookEventType);

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual(0, spy.CallCount, "Dispatcher must not be called for malformed payloads.");
        }

        // ── unsupported event type ────────────────────────────────────────────────────

        /// <summary>
        /// A POST with an unsupported X-Gitlab-Event value (e.g. "Push Hook") must return 200
        /// and must not invoke the dispatcher.
        /// </summary>
        [TestMethod]
        public async Task Post_UnsupportedEventType_Returns200WithoutDispatching()
        {
            var spy = new DispatcherSpy();
            using var client = CreateClient(spy);

            var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
            {
                Content = IssuePayload("open")
            };
            request.Headers.Add(GitLabTokenHeader, ValidToken);
            request.Headers.Add(GitLabEventHeader, "Push Hook");

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(0, spy.CallCount, "Dispatcher must not be called for unsupported event types.");
        }

        /// <summary>
        /// A POST with a valid "Issue Hook" but an unsupported action (e.g. "close") must return
        /// 200 and must not invoke the dispatcher.
        /// </summary>
        [TestMethod]
        public async Task Post_IssueHookWithIgnoredAction_Returns200WithoutDispatching()
        {
            var spy = new DispatcherSpy();
            using var client = CreateClient(spy);

            var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
            {
                Content = IssuePayload("close")
            };
            request.Headers.Add(GitLabTokenHeader, ValidToken);
            request.Headers.Add(GitLabEventHeader, IssueHookEventType);

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(0, spy.CallCount, "Dispatcher must not be called for ignored actions.");
        }

        // ── dispatcher spy ────────────────────────────────────────────────────────────

        /// <summary>
        /// Simple test spy that records calls to <see cref="IIssueEventDispatcher.DispatchAsync"/>.
        /// </summary>
        private sealed class DispatcherSpy : IIssueEventDispatcher
        {
            public int CallCount { get; private set; }
            public GitLabIssueEvent? LastEvent { get; private set; }

            public Task DispatchAsync(GitLabIssueEvent issueEvent, CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastEvent = issueEvent;
                return Task.CompletedTask;
            }
        }
    }
}
