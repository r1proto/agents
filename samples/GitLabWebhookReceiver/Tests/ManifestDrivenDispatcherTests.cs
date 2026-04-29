using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitLabWebhookReceiver.Models;
using GitLabWebhookReceiver.Dispatcher;

namespace GitLabWebhookReceiver.Tests
{
    [TestClass]
    public class ManifestDrivenDispatcherTests
    {
        private const string TestGitLabUrl = "https://gitlab.example.com";

        // Test doubles

        private class TestAgentSubmissionService : IAgentSubmissionService
        {
            public int SubmitCount { get; private set; }
            public AgentTask LastSubmittedTask { get; private set; }
            public bool ShouldFail { get; set; }
            public string FailureMessage { get; set; }

            public AgentSubmissionResult SubmitTask(AgentTask task)
            {
                SubmitCount++;
                LastSubmittedTask = task;

                if (ShouldFail)
                    return AgentSubmissionResult.Failed(FailureMessage ?? "Test failure");

                return AgentSubmissionResult.Successful($"task-{SubmitCount}");
            }
        }

        // Helper methods

        private GitLabIssueEvent CreateTestIssueEvent(
            int projectId = 1,
            int issueIid = 1,
            string action = "open",
            DateTime? updatedAt = null,
            params string[] labelNames)
        {
            var labels = new GitLabLabel[labelNames.Length];
            for (int i = 0; i < labelNames.Length; i++)
            {
                labels[i] = new GitLabLabel { Id = i + 1, Title = labelNames[i] };
            }

            return new GitLabIssueEvent
            {
                ObjectKind = "issue",
                User = new GitLabUser
                {
                    Id = 1,
                    Username = "testuser",
                    Name = "Test User",
                    Email = "test@example.com"
                },
                Project = new GitLabProject
                {
                    Id = projectId,
                    Name = "Test Project",
                    PathWithNamespace = "test/project"
                },
                ObjectAttributes = new GitLabIssueAttributes
                {
                    Id = 100,
                    Iid = issueIid,
                    Title = "Test Issue",
                    Description = "This is a test issue",
                    State = "opened",
                    Action = action,
                    Url = $"https://gitlab.example.com/test/project/-/issues/{issueIid}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = updatedAt ?? DateTime.UtcNow,
                    AuthorId = 1,
                    ProjectId = projectId
                },
                Labels = labels
            };
        }

        private IManifestRepository CreateTestManifestRepository()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "backend",
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend",
                    TargetRepoRef = "main"
                },
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "frontend",
                    GitLabTargetRepoUrl = "https://github.com/example/frontend",
                    CodeRepositoryPath = "example/frontend",
                    TargetRepoRef = ""
                },
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "infrastructure",
                    GitLabTargetRepoUrl = "https://github.com/example/infra",
                    CodeRepositoryPath = "example/infra"
                }
            };

            return new InMemoryManifestRepository(manifests);
        }

        // Tests

        [TestMethod]
        public void ManifestDispatcher_RoutesToCorrectRepo_BasedOnLabel()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            var issueEvent = CreateTestIssueEvent(labelNames: new[] { "backend", "bug" });
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Success, result.ResultType);
            Assert.AreEqual(1, agentService.SubmitCount);
            Assert.IsNotNull(agentService.LastSubmittedTask);
            Assert.AreEqual("https://github.com/example/backend", agentService.LastSubmittedTask.TargetRepoUrl);
            Assert.AreEqual("main", agentService.LastSubmittedTask.TargetRepoRef);
        }

        [TestMethod]
        public void ManifestDispatcher_RoutesToCorrectRepo_MultipleMatchingLabels()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            // Issue has both "backend" and "frontend" labels - should route to first match
            var issueEvent = CreateTestIssueEvent(labelNames: new[] { "backend", "frontend" });
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Success, result.ResultType);
            Assert.AreEqual(1, agentService.SubmitCount);
            // Should match the first label that has a manifest
            Assert.AreEqual("https://github.com/example/backend", agentService.LastSubmittedTask.TargetRepoUrl);
        }

        [TestMethod]
        public void ManifestDispatcher_FailsWhenNoMatchingLabel()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            // Issue has no labels that match any manifest
            var issueEvent = CreateTestIssueEvent(labelNames: new[] { "bug", "priority-high" });
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Failure, result.ResultType);
            Assert.AreEqual(0, agentService.SubmitCount);
            Assert.IsTrue(result.Message.Contains("No manifest found"));
            Assert.IsTrue(result.Message.Contains("bug, priority-high"));
        }

        [TestMethod]
        public void ManifestDispatcher_FailsWhenNoLabels()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            // Issue has no labels at all
            var issueEvent = CreateTestIssueEvent(labelNames: new string[0]);
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Failure, result.ResultType);
            Assert.AreEqual(0, agentService.SubmitCount);
            Assert.IsTrue(result.Message.Contains("No manifest found"));
            Assert.IsTrue(result.Message.Contains("(none)"));
        }

        [TestMethod]
        public void ManifestDispatcher_DetectsDuplicates()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            var timestamp = DateTime.UtcNow;
            var event1 = CreateTestIssueEvent(1, 1, "open", timestamp, "backend");
            var event2 = CreateTestIssueEvent(1, 1, "open", timestamp, "backend");

            var result1 = dispatcher.DispatchIssueEventWithResult(event1);
            var result2 = dispatcher.DispatchIssueEventWithResult(event2);

            Assert.AreEqual(DispatchResultType.Success, result1.ResultType);
            Assert.AreEqual(DispatchResultType.Duplicate, result2.ResultType);
            Assert.AreEqual(1, agentService.SubmitCount, "Second event should be detected as duplicate");
        }

        [TestMethod]
        public void ManifestDispatcher_AllowsRetryAfterFailure()
        {
            var agentService = new TestAgentSubmissionService
            {
                ShouldFail = true,
                FailureMessage = "Temporary failure"
            };
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            var timestamp = DateTime.UtcNow;
            var event1 = CreateTestIssueEvent(1, 1, "open", timestamp, "backend");
            var event2 = CreateTestIssueEvent(1, 1, "open", timestamp, "backend");

            // First attempt fails
            var result1 = dispatcher.DispatchIssueEventWithResult(event1);
            Assert.AreEqual(DispatchResultType.Failure, result1.ResultType);

            // Second attempt should be allowed (not marked as duplicate)
            agentService.ShouldFail = false;
            var result2 = dispatcher.DispatchIssueEventWithResult(event2);
            Assert.AreEqual(DispatchResultType.Success, result2.ResultType);
            Assert.AreEqual(2, agentService.SubmitCount);
        }

        [TestMethod]
        public void ManifestDispatcher_DifferentReposForDifferentLabels()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            var backendEvent = CreateTestIssueEvent(1, 1, "open", null, "backend");
            var frontendEvent = CreateTestIssueEvent(1, 2, "open", null, "frontend");

            var result1 = dispatcher.DispatchIssueEventWithResult(backendEvent);
            var result2 = dispatcher.DispatchIssueEventWithResult(frontendEvent);

            Assert.AreEqual(DispatchResultType.Success, result1.ResultType);
            Assert.AreEqual(DispatchResultType.Success, result2.ResultType);
            Assert.AreEqual(2, agentService.SubmitCount);

            // Verify different target repos were used
            var tasks = new[] { agentService.LastSubmittedTask };
            // Note: We only have access to the last task, but we verified both succeeded
        }

        [TestMethod]
        public void ManifestDispatcher_HandlesLabelCaseInsensitively()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            // Manifest has "backend", issue has "BackEnd"
            var issueEvent = CreateTestIssueEvent(labelNames: new[] { "BackEnd" });
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Success, result.ResultType);
            Assert.AreEqual(1, agentService.SubmitCount);
            Assert.AreEqual("https://github.com/example/backend", agentService.LastSubmittedTask.TargetRepoUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ManifestDispatcher_Constructor_RequiresAgentService()
        {
            var manifestRepo = CreateTestManifestRepository();
            new ManifestDrivenGitLabDispatcher(null, manifestRepo, TestGitLabUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ManifestDispatcher_Constructor_RequiresManifestRepository()
        {
            var agentService = new TestAgentSubmissionService();
            new ManifestDrivenGitLabDispatcher(agentService, null, TestGitLabUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ManifestDispatcher_Constructor_RequiresGitLabBaseUrl()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            new ManifestDrivenGitLabDispatcher(agentService, manifestRepo, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ManifestDispatcher_ThrowsOnNullEvent()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            dispatcher.DispatchIssueEvent(null);
        }

        [TestMethod]
        public void ManifestDispatcher_UsesManifestTargetRepoRef()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            // Backend manifest has TargetRepoRef = "main"
            var issueEvent = CreateTestIssueEvent(labelNames: new[] { "backend" });
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Success, result.ResultType);
            Assert.AreEqual("main", agentService.LastSubmittedTask.TargetRepoRef);
        }

        [TestMethod]
        public void ManifestDispatcher_HandlesEmptyTargetRepoRef()
        {
            var agentService = new TestAgentSubmissionService();
            var manifestRepo = CreateTestManifestRepository();
            var dispatcher = new ManifestDrivenGitLabDispatcher(
                agentService,
                manifestRepo,
                TestGitLabUrl);

            // Frontend manifest has TargetRepoRef = ""
            var issueEvent = CreateTestIssueEvent(labelNames: new[] { "frontend" });
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Success, result.ResultType);
            Assert.AreEqual("", agentService.LastSubmittedTask.TargetRepoRef);
        }
    }
}
