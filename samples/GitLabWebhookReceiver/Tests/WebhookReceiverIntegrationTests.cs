using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using GitLabWebhookReceiver.Models;
using GitLabWebhookReceiver.Dispatcher;

namespace GitLabWebhookReceiver.Tests
{
    /// <summary>
    /// Integration tests for the GitLab webhook receiver.
    /// These tests verify the complete HTTP request/response flow.
    /// </summary>
    [TestClass]
    public class WebhookReceiverIntegrationTests
    {
        private const string ValidSecret = "test-secret-token-integration";
        private const string TestHost = "localhost";
        private const int TestPort = 8081; // Use a different port to avoid conflicts
        private const string WebhookUrl = "http://localhost:8081/webhooks/gitlab/issues/";

        private WebhookReceiver.WebhookServer _server;
        private Thread _serverThread;
        private TestDispatcher _dispatcher;

        private class TestDispatcher : IIssueEventDispatcher
        {
            private readonly object _lock = new object();
            private int _dispatchCount = 0;
            private GitLabIssueEvent _lastEvent = null;

            public int DispatchCount
            {
                get { lock (_lock) { return _dispatchCount; } }
            }

            public GitLabIssueEvent LastEvent
            {
                get { lock (_lock) { return _lastEvent; } }
            }

            public void DispatchIssueEvent(GitLabIssueEvent issueEvent)
            {
                lock (_lock)
                {
                    _dispatchCount++;
                    _lastEvent = issueEvent;
                }
            }

            public void Reset()
            {
                lock (_lock)
                {
                    _dispatchCount = 0;
                    _lastEvent = null;
                }
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _dispatcher = new TestDispatcher();
            _server = new WebhookReceiver.WebhookServer(TestHost, TestPort, _dispatcher, ValidSecret);

            // Start server in background thread
            _serverThread = new Thread(() =>
            {
                try
                {
                    _server.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server thread exception: {ex.Message}");
                }
            });
            _serverThread.IsBackground = true;
            _serverThread.Start();

            // Give server time to start
            Thread.Sleep(500);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _server?.Stop();
            if (_serverThread != null && _serverThread.IsAlive)
            {
                _serverThread.Join(1000);
            }
        }

        [TestMethod]
        public void ValidToken_IssueOpenEvent_Returns200AndDispatchesEvent()
        {
            var issueEvent = CreateValidIssueEvent("open");
            var json = JsonConvert.SerializeObject(issueEvent);

            var response = SendWebhookRequest(json, ValidSecret, "Issue Hook");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Expected 200 OK");
            Assert.AreEqual(1, _dispatcher.DispatchCount, "Expected dispatcher to be called once");
            Assert.IsNotNull(_dispatcher.LastEvent, "Expected event to be dispatched");
            Assert.AreEqual("open", _dispatcher.LastEvent.ObjectAttributes.Action);
        }

        [TestMethod]
        public void ValidToken_IssueUpdateEvent_Returns200AndDispatchesEvent()
        {
            var issueEvent = CreateValidIssueEvent("update");
            var json = JsonConvert.SerializeObject(issueEvent);

            var response = SendWebhookRequest(json, ValidSecret, "Issue Hook");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Expected 200 OK");
            Assert.AreEqual(1, _dispatcher.DispatchCount, "Expected dispatcher to be called once");
            Assert.AreEqual("update", _dispatcher.LastEvent.ObjectAttributes.Action);
        }

        [TestMethod]
        public void InvalidToken_Returns401()
        {
            var issueEvent = CreateValidIssueEvent("open");
            var json = JsonConvert.SerializeObject(issueEvent);

            var response = SendWebhookRequest(json, "wrong-token", "Issue Hook");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Expected 401 Unauthorized");
            Assert.AreEqual(0, _dispatcher.DispatchCount, "Expected dispatcher not to be called");
        }

        [TestMethod]
        public void MissingToken_Returns401()
        {
            var issueEvent = CreateValidIssueEvent("open");
            var json = JsonConvert.SerializeObject(issueEvent);

            var response = SendWebhookRequest(json, null, "Issue Hook");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Expected 401 Unauthorized");
            Assert.AreEqual(0, _dispatcher.DispatchCount, "Expected dispatcher not to be called");
        }

        [TestMethod]
        public void MalformedJson_Returns400()
        {
            var malformedJson = "{ this is not valid json }";

            var response = SendWebhookRequest(malformedJson, ValidSecret, "Issue Hook");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Expected 400 Bad Request");
            Assert.AreEqual(0, _dispatcher.DispatchCount, "Expected dispatcher not to be called");
        }

        [TestMethod]
        public void UnsupportedEventType_Returns200WithoutDispatching()
        {
            var issueEvent = CreateValidIssueEvent("open");
            var json = JsonConvert.SerializeObject(issueEvent);

            var response = SendWebhookRequest(json, ValidSecret, "Push Hook");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Expected 200 OK for unsupported event");
            Assert.AreEqual(0, _dispatcher.DispatchCount, "Expected dispatcher not to be called for unsupported event");
        }

        [TestMethod]
        public void UnsupportedAction_Returns200WithoutDispatching()
        {
            var issueEvent = CreateValidIssueEvent("close");
            var json = JsonConvert.SerializeObject(issueEvent);

            var response = SendWebhookRequest(json, ValidSecret, "Issue Hook");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Expected 200 OK for unsupported action");
            Assert.AreEqual(0, _dispatcher.DispatchCount, "Expected dispatcher not to be called for unsupported action");
        }

        [TestMethod]
        public void GetRequest_Returns405()
        {
            var response = SendGetRequest();

            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode, "Expected 405 Method Not Allowed");
            Assert.AreEqual(0, _dispatcher.DispatchCount, "Expected dispatcher not to be called for GET request");
        }

        // Helper methods

        private GitLabIssueEvent CreateValidIssueEvent(string action)
        {
            return new GitLabIssueEvent
            {
                ObjectKind = "issue",
                User = new GitLabUser
                {
                    Id = 1,
                    Username = "integrationtest",
                    Name = "Integration Test User"
                },
                Project = new GitLabProject
                {
                    Id = 1,
                    Name = "Test Project",
                    PathWithNamespace = "test/project",
                    WebUrl = "https://gitlab.example.com/test/project"
                },
                ObjectAttributes = new GitLabIssueAttributes
                {
                    Id = 100,
                    Iid = 1,
                    Title = "Integration Test Issue",
                    Description = "This is an integration test issue",
                    State = "opened",
                    Action = action,
                    Url = "https://gitlab.example.com/test/project/-/issues/1",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AuthorId = 1,
                    ProjectId = 1
                }
            };
        }

        private HttpWebResponse SendWebhookRequest(string body, string token, string eventType)
        {
            var request = (HttpWebRequest)WebRequest.Create(WebhookUrl);
            request.Method = "POST";
            request.ContentType = "application/json";

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers["X-Gitlab-Token"] = token;
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                request.Headers["X-Gitlab-Event"] = eventType;
            }

            var bodyBytes = Encoding.UTF8.GetBytes(body);
            request.ContentLength = bodyBytes.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                // For error status codes, the response is in the exception
                return (HttpWebResponse)ex.Response;
            }
        }

        private HttpWebResponse SendGetRequest()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebhookUrl);
            request.Method = "GET";
            request.Headers["X-Gitlab-Token"] = ValidSecret;

            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                return (HttpWebResponse)ex.Response;
            }
        }
    }
}
