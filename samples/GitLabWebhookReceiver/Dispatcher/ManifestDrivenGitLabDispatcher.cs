using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Dispatcher
{
    /// <summary>
    /// Manifest-driven GitLab issue event dispatcher that resolves target repositories
    /// from GitLab group repo manifests based on issue labels, replacing app-config lookup.
    /// </summary>
    public class ManifestDrivenGitLabDispatcher : IIssueEventDispatcher
    {
        private readonly IAgentSubmissionService _agentSubmissionService;
        private readonly IManifestRepository _manifestRepository;
        private readonly string _gitlabBaseUrl;
        private readonly HashSet<string> _processedKeys;
        private readonly object _lockObject = new object();

        // In production, this would be replaced with a distributed cache (Redis, etc.)
        // with TTL expiration. For now, we use an in-memory set with a size limit.
        private const int MaxCacheSize = 10000;

        public ManifestDrivenGitLabDispatcher(
            IAgentSubmissionService agentSubmissionService,
            IManifestRepository manifestRepository,
            string gitlabBaseUrl)
        {
            _agentSubmissionService = agentSubmissionService ?? throw new ArgumentNullException(nameof(agentSubmissionService));
            _manifestRepository = manifestRepository ?? throw new ArgumentNullException(nameof(manifestRepository));
            _gitlabBaseUrl = gitlabBaseUrl ?? throw new ArgumentNullException(nameof(gitlabBaseUrl));
            _processedKeys = new HashSet<string>();
        }

        /// <summary>
        /// Dispatches a GitLab issue event to the agent with idempotency checks.
        /// Uses manifest-based routing to determine the target repository.
        /// </summary>
        public void DispatchIssueEvent(GitLabIssueEvent issueEvent)
        {
            var result = DispatchIssueEventWithResult(issueEvent);

            // Log the result
            switch (result.ResultType)
            {
                case DispatchResultType.Success:
                    Console.WriteLine($"[ManifestDispatcher] {result.Message} (TaskId: {result.TaskId})");
                    break;
                case DispatchResultType.Duplicate:
                    Console.WriteLine($"[ManifestDispatcher] {result.Message}");
                    break;
                case DispatchResultType.Failure:
                    Console.WriteLine($"[ManifestDispatcher] ERROR: {result.Message}");
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
                // Resolve target repository from manifest based on issue labels
                var manifest = ResolveManifestFromLabels(issueEvent);
                if (manifest == null)
                {
                    var labels = GetLabelNames(issueEvent);
                    var labelsStr = labels.Length > 0 ? string.Join(", ", labels) : "(none)";
                    return DispatchResult.Failure(
                        $"No manifest found for any issue labels: {labelsStr}. " +
                        "Issue must have a label matching a configured manifest tag.");
                }

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

                // Convert to internal task payload using manifest-resolved target repo
                var task = AgentTask.FromGitLabIssueEvent(
                    issueEvent,
                    _gitlabBaseUrl,
                    manifest.GitLabTargetRepoUrl,
                    manifest.TargetRepoRef ?? "");

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
                Console.WriteLine($"[ManifestDispatcher] Exception during dispatch: {ex.Message}");
                Console.WriteLine($"[ManifestDispatcher] Stack trace: {ex.StackTrace}");
                return DispatchResult.Failure($"Exception during dispatch: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolves a manifest by finding the first matching issue label.
        /// Returns null if no manifest matches any of the issue's labels.
        /// </summary>
        private GitLabGroupRepoManifest ResolveManifestFromLabels(GitLabIssueEvent issueEvent)
        {
            var labels = GetLabelNames(issueEvent);

            // Try to find a manifest for any of the issue's labels
            foreach (var label in labels)
            {
                var manifest = _manifestRepository.FindByIssueTag(label);
                if (manifest != null)
                    return manifest;
            }

            return null;
        }

        /// <summary>
        /// Extracts label names from the issue event.
        /// </summary>
        private string[] GetLabelNames(GitLabIssueEvent issueEvent)
        {
            var labels = issueEvent.Labels ?? new GitLabLabel[0];
            return labels.Where(l => l != null && !string.IsNullOrWhiteSpace(l.Title))
                         .Select(l => l.Title)
                         .ToArray();
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
