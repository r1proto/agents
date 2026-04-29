using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Dispatcher
{
    /// <summary>
    /// Result of dispatching a GitLab issue event.
    /// </summary>
    public enum DispatchResultType
    {
        /// <summary>Event was successfully dispatched to the agent</summary>
        Success,
        /// <summary>Event was a duplicate and was not dispatched</summary>
        Duplicate,
        /// <summary>Dispatch failed due to an error</summary>
        Failure
    }

    /// <summary>
    /// Result of a dispatch operation.
    /// </summary>
    public class DispatchResult
    {
        public DispatchResultType ResultType { get; set; }
        public string Message { get; set; }
        public string TaskId { get; set; }

        public static DispatchResult Success(string taskId = null)
        {
            return new DispatchResult
            {
                ResultType = DispatchResultType.Success,
                Message = "Event dispatched successfully",
                TaskId = taskId
            };
        }

        public static DispatchResult Duplicate(string deduplicationKey)
        {
            return new DispatchResult
            {
                ResultType = DispatchResultType.Duplicate,
                Message = $"Event is a duplicate (key: {deduplicationKey})"
            };
        }

        public static DispatchResult Failure(string errorMessage)
        {
            return new DispatchResult
            {
                ResultType = DispatchResultType.Failure,
                Message = errorMessage
            };
        }
    }

    /// <summary>
    /// GitLab-specific dispatcher that converts GitLab issue events into internal tasks
    /// and submits them to the agent with idempotency checks.
    /// </summary>
    public class GitLabIssueEventDispatcher : IIssueEventDispatcher
    {
        private readonly IAgentSubmissionService _agentSubmissionService;
        private readonly string _gitlabBaseUrl;
        private readonly string _targetRepoUrl;
        private readonly string _targetRepoRef;
        private readonly HashSet<string> _processedKeys;
        private readonly object _lockObject = new object();

        // In production, this would be replaced with a distributed cache (Redis, etc.)
        // with TTL expiration. For now, we use an in-memory set with a size limit.
        private const int MaxCacheSize = 10000;

        public GitLabIssueEventDispatcher(
            IAgentSubmissionService agentSubmissionService,
            string gitlabBaseUrl,
            string targetRepoUrl,
            string targetRepoRef = "")
        {
            _agentSubmissionService = agentSubmissionService ?? throw new ArgumentNullException(nameof(agentSubmissionService));
            _gitlabBaseUrl = gitlabBaseUrl ?? throw new ArgumentNullException(nameof(gitlabBaseUrl));
            _targetRepoUrl = targetRepoUrl ?? throw new ArgumentNullException(nameof(targetRepoUrl));
            _targetRepoRef = targetRepoRef ?? "";
            _processedKeys = new HashSet<string>();
        }

        /// <summary>
        /// Dispatches a GitLab issue event to the agent with idempotency checks.
        /// </summary>
        public void DispatchIssueEvent(GitLabIssueEvent issueEvent)
        {
            var result = DispatchIssueEventWithResult(issueEvent);

            // Log the result
            switch (result.ResultType)
            {
                case DispatchResultType.Success:
                    Console.WriteLine($"[Dispatcher] {result.Message} (TaskId: {result.TaskId})");
                    break;
                case DispatchResultType.Duplicate:
                    Console.WriteLine($"[Dispatcher] {result.Message}");
                    break;
                case DispatchResultType.Failure:
                    Console.WriteLine($"[Dispatcher] ERROR: {result.Message}");
                    break;
            }
        }

        /// <summary>
        /// Dispatches a GitLab issue event and returns a detailed result.
        /// This method is primarily for testing purposes.
        /// </summary>
        public DispatchResult DispatchIssueEventWithResult(GitLabIssueEvent issueEvent)
        {
            if (issueEvent == null)
                throw new ArgumentNullException(nameof(issueEvent));

            try
            {
                // Generate deduplication key
                var deduplicationKey = GenerateDeduplicationKey(issueEvent);

                // Check for duplicates
                lock (_lockObject)
                {
                    if (_processedKeys.Contains(deduplicationKey))
                    {
                        return DispatchResult.Duplicate(deduplicationKey);
                    }

                    // Add to processed keys
                    _processedKeys.Add(deduplicationKey);

                    // Limit cache size (simple LRU-like behavior)
                    if (_processedKeys.Count > MaxCacheSize)
                    {
                        // In production, use a proper LRU cache with TTL
                        // For now, clear half the cache when limit is reached
                        var toRemove = new List<string>();
                        var count = 0;
                        foreach (var key in _processedKeys)
                        {
                            toRemove.Add(key);
                            if (++count >= MaxCacheSize / 2)
                                break;
                        }
                        foreach (var key in toRemove)
                        {
                            _processedKeys.Remove(key);
                        }
                    }
                }

                // Convert to internal task payload
                var task = AgentTask.FromGitLabIssueEvent(
                    issueEvent,
                    _gitlabBaseUrl,
                    _targetRepoUrl,
                    _targetRepoRef);

                // Submit to agent
                var submissionResult = _agentSubmissionService.SubmitTask(task);

                if (!submissionResult.Success)
                {
                    // Remove from cache if submission failed, so it can be retried
                    lock (_lockObject)
                    {
                        _processedKeys.Remove(deduplicationKey);
                    }
                    return DispatchResult.Failure(submissionResult.ErrorMessage);
                }

                return DispatchResult.Success(submissionResult.TaskId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dispatcher] Exception during dispatch: {ex.Message}");
                Console.WriteLine($"[Dispatcher] Stack trace: {ex.StackTrace}");
                return DispatchResult.Failure($"Exception during dispatch: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a deterministic deduplication key from the event.
        /// Key format: SHA256(source_project_id + issue_iid + action + timestamp)
        /// </summary>
        private string GenerateDeduplicationKey(GitLabIssueEvent issueEvent)
        {
            var attrs = issueEvent.ObjectAttributes;
            var project = issueEvent.Project;

            if (attrs == null || project == null)
                throw new ArgumentException("Event is missing required attributes", nameof(issueEvent));

            // Use updated_at as the timestamp (more stable than created_at for updates)
            // Truncate to seconds to avoid sub-second duplicates
            var timestamp = attrs.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss");

            // Construct the key components
            var keyData = $"{project.Id}:{attrs.Iid}:{attrs.Action}:{timestamp}";

            // Hash the key for a stable, fixed-length identifier
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyData));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
