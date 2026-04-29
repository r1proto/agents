using System;
using System.Collections.Generic;
using System.Linq;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Dispatcher
{
    /// <summary>
    /// In-memory manifest repository. In production, this would load manifests
    /// from a configuration file, database, or GitLab API.
    /// </summary>
    public class InMemoryManifestRepository : IManifestRepository
    {
        private readonly Dictionary<string, GitLabGroupRepoManifest> _manifestsByTag;

        public InMemoryManifestRepository(GitLabGroupRepoManifest[] manifests)
        {
            if (manifests == null)
                throw new ArgumentNullException(nameof(manifests));

            _manifestsByTag = new Dictionary<string, GitLabGroupRepoManifest>(StringComparer.OrdinalIgnoreCase);

            foreach (var manifest in manifests)
            {
                var validationError = manifest?.Validate();
                if (validationError != null)
                    throw new ArgumentException($"Invalid manifest: {validationError}");

                if (_manifestsByTag.ContainsKey(manifest.GitLabIssueTag))
                    throw new ArgumentException($"Duplicate manifest for tag: {manifest.GitLabIssueTag}");

                _manifestsByTag[manifest.GitLabIssueTag] = manifest;
            }
        }

        public GitLabGroupRepoManifest FindByIssueTag(string issueTag)
        {
            if (string.IsNullOrWhiteSpace(issueTag))
                return null;

            return _manifestsByTag.TryGetValue(issueTag, out var manifest) ? manifest : null;
        }

        public GitLabGroupRepoManifest[] GetAll()
        {
            return _manifestsByTag.Values.ToArray();
        }
    }
}
