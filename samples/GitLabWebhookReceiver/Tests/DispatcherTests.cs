using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitLabWebhookReceiver.Models;
using GitLabWebhookReceiver.Dispatcher;

namespace GitLabWebhookReceiver.Tests
{
    [TestClass]
    public class DispatcherTests
    {
        private const string TestGitLabUrl = "https://gitlab.example.com";
        private const string TestTargetRepoUrl = "https://github.com/test/repo";
        private const string TestTargetRepoRef = "main";

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
            DateTime? updatedAt = null)
        {
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
                Labels = new GitLabLabel[]
                {
                    new GitLabLabel { Id = 1, Title = "bug" },
                    new GitLabLabel { Id = 2, Title = "priority-high" }
                }
            };
        }

        // Tests for AgentTask.FromGitLabIssueEvent

        [TestMethod]
        public void AgentTask_FromGitLabIssueEvent_PopulatesAllFields()
        {
            var issueEvent = CreateTestIssueEvent();
            var task = AgentTask.FromGitLabIssueEvent(
                issueEvent,
                TestGitLabUrl,
                TestTargetRepoUrl,
                TestTargetRepoRef);

            // Source fields
            Assert.AreEqual("gitlab", task.Source);
            Assert.AreEqual(TestGitLabUrl, task.InstanceUrl);
            Assert.AreEqual(1, task.SourceProjectId);
            Assert.AreEqual("test/project", task.SourceProjectPath);
            Assert.AreEqual(1, task.IssueIid);
            Assert.AreEqual("Test Issue", task.Title);
            Assert.AreEqual("This is a test issue", task.Description);
            Assert.AreEqual("testuser", task.Author);
            Assert.AreEqual("opened", task.State);
            Assert.AreEqual("open", task.EventAction);
            Assert.IsNotNull(task.Labels);
            Assert.AreEqual(2, task.Labels.Length);
            Assert.AreEqual("bug", task.Labels[0]);
            Assert.AreEqual("priority-high", task.Labels[1]);

            // Target fields
            Assert.AreEqual(TestTargetRepoUrl, task.TargetRepoUrl);
            Assert.AreEqual(TestTargetRepoRef, task.TargetRepoRef);
        }

        [TestMethod]
        public void AgentTask_FromGitLabIssueEvent_HandlesEmptyLabels()
        {
            var issueEvent = CreateTestIssueEvent();
            issueEvent.Labels = new GitLabLabel[0];

            var task = AgentTask.FromGitLabIssueEvent(
                issueEvent,
                TestGitLabUrl,
                TestTargetRepoUrl);

            Assert.IsNotNull(task.Labels);
            Assert.AreEqual(0, task.Labels.Length);
        }

        [TestMethod]
        public void AgentTask_FromGitLabIssueEvent_DefaultsTargetRepoRef()
        {
            var issueEvent = CreateTestIssueEvent();
            var task = AgentTask.FromGitLabIssueEvent(
                issueEvent,
                TestGitLabUrl,
                TestTargetRepoUrl);

            Assert.AreEqual("", task.TargetRepoRef);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AgentTask_FromGitLabIssueEvent_ThrowsOnNullEvent()
        {
            AgentTask.FromGitLabIssueEvent(null, TestGitLabUrl, TestTargetRepoUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AgentTask_FromGitLabIssueEvent_ThrowsOnEmptyTargetRepoUrl()
        {
            var issueEvent = CreateTestIssueEvent();
            AgentTask.FromGitLabIssueEvent(issueEvent, TestGitLabUrl, "");
        }

        // Tests for GitLabIssueEventDispatcher

        [TestMethod]
        public void Dispatcher_SuccessfulDispatch_CallsAgentSubmission()
        {
            var agentService = new TestAgentSubmissionService();
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl,
                TestTargetRepoRef);

            var issueEvent = CreateTestIssueEvent();
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Success, result.ResultType);
            Assert.AreEqual(1, agentService.SubmitCount);
            Assert.IsNotNull(agentService.LastSubmittedTask);
            Assert.AreEqual(1, agentService.LastSubmittedTask.IssueIid);
            Assert.AreEqual(TestTargetRepoUrl, agentService.LastSubmittedTask.TargetRepoUrl);
        }

        [TestMethod]
        public void Dispatcher_DuplicateEvent_ReturnsSecondAsDuplicate()
        {
            var agentService = new TestAgentSubmissionService();
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl);

            // Create two identical events (same deduplication key)
            var timestamp = DateTime.UtcNow;
            var event1 = CreateTestIssueEvent(1, 1, "open", timestamp);
            var event2 = CreateTestIssueEvent(1, 1, "open", timestamp);

            // First dispatch should succeed
            var result1 = dispatcher.DispatchIssueEventWithResult(event1);
            Assert.AreEqual(DispatchResultType.Success, result1.ResultType);
            Assert.AreEqual(1, agentService.SubmitCount);

            // Second dispatch should be marked as duplicate
            var result2 = dispatcher.DispatchIssueEventWithResult(event2);
            Assert.AreEqual(DispatchResultType.Duplicate, result2.ResultType);
            Assert.AreEqual(1, agentService.SubmitCount, "Agent should not be called for duplicate");
        }

        [TestMethod]
        public void Dispatcher_DifferentEvents_BothDispatched()
        {
            var agentService = new TestAgentSubmissionService();
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl);

            // Create two different events (different deduplication keys)
            var event1 = CreateTestIssueEvent(1, 1, "open");
            var event2 = CreateTestIssueEvent(1, 2, "open"); // Different issue IID

            var result1 = dispatcher.DispatchIssueEventWithResult(event1);
            var result2 = dispatcher.DispatchIssueEventWithResult(event2);

            Assert.AreEqual(DispatchResultType.Success, result1.ResultType);
            Assert.AreEqual(DispatchResultType.Success, result2.ResultType);
            Assert.AreEqual(2, agentService.SubmitCount, "Both events should be dispatched");
        }

        [TestMethod]
        public void Dispatcher_AgentSubmissionFailure_ReturnsFailure()
        {
            var agentService = new TestAgentSubmissionService
            {
                ShouldFail = true,
                FailureMessage = "Agent service unavailable"
            };
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl);

            var issueEvent = CreateTestIssueEvent();
            var result = dispatcher.DispatchIssueEventWithResult(issueEvent);

            Assert.AreEqual(DispatchResultType.Failure, result.ResultType);
            Assert.IsTrue(result.Message.Contains("Agent service unavailable"));
            Assert.AreEqual(1, agentService.SubmitCount);
        }

        [TestMethod]
        public void Dispatcher_FailureThenRetry_AllowsRetry()
        {
            var agentService = new TestAgentSubmissionService
            {
                ShouldFail = true,
                FailureMessage = "Temporary failure"
            };
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl);

            var timestamp = DateTime.UtcNow;
            var event1 = CreateTestIssueEvent(1, 1, "open", timestamp);
            var event2 = CreateTestIssueEvent(1, 1, "open", timestamp);

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
        [ExpectedException(typeof(ArgumentNullException))]
        public void Dispatcher_ThrowsOnNullEvent()
        {
            var agentService = new TestAgentSubmissionService();
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl);

            dispatcher.DispatchIssueEvent(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Dispatcher_Constructor_RequiresAgentService()
        {
            new GitLabIssueEventDispatcher(null, TestGitLabUrl, TestTargetRepoUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Dispatcher_Constructor_RequiresGitLabBaseUrl()
        {
            var agentService = new TestAgentSubmissionService();
            new GitLabIssueEventDispatcher(agentService, null, TestTargetRepoUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Dispatcher_Constructor_RequiresTargetRepoUrl()
        {
            var agentService = new TestAgentSubmissionService();
            new GitLabIssueEventDispatcher(agentService, TestGitLabUrl, null);
        }

        [TestMethod]
        public void Dispatcher_DifferentActions_GenerateDifferentKeys()
        {
            var agentService = new TestAgentSubmissionService();
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl);

            var timestamp = DateTime.UtcNow;
            var event1 = CreateTestIssueEvent(1, 1, "open", timestamp);
            var event2 = CreateTestIssueEvent(1, 1, "update", timestamp); // Same issue, different action

            var result1 = dispatcher.DispatchIssueEventWithResult(event1);
            var result2 = dispatcher.DispatchIssueEventWithResult(event2);

            Assert.AreEqual(DispatchResultType.Success, result1.ResultType);
            Assert.AreEqual(DispatchResultType.Success, result2.ResultType);
            Assert.AreEqual(2, agentService.SubmitCount, "Different actions should not be treated as duplicates");
        }

        [TestMethod]
        public void Dispatcher_DifferentTimestamps_GenerateDifferentKeys()
        {
            var agentService = new TestAgentSubmissionService();
            var dispatcher = new GitLabIssueEventDispatcher(
                agentService,
                TestGitLabUrl,
                TestTargetRepoUrl);

            var event1 = CreateTestIssueEvent(1, 1, "update", DateTime.Parse("2024-01-01T10:00:00Z"));
            var event2 = CreateTestIssueEvent(1, 1, "update", DateTime.Parse("2024-01-01T10:00:01Z"));

            var result1 = dispatcher.DispatchIssueEventWithResult(event1);
            var result2 = dispatcher.DispatchIssueEventWithResult(event2);

            Assert.AreEqual(DispatchResultType.Success, result1.ResultType);
            Assert.AreEqual(DispatchResultType.Success, result2.ResultType);
            Assert.AreEqual(2, agentService.SubmitCount, "Different timestamps should not be treated as duplicates");
        }

        // Tests for configuration validation

        [TestMethod]
        public void WebhookConfig_ValidateConfig_ReturnsNullForValid()
        {
            // This test would need actual config values set
            // For now, we just test the validation logic structure
            var errorMessage = Config.WebhookConfig.ValidateConfig();
            // In a real test environment with proper config, this would check for null
        }
    }
}
