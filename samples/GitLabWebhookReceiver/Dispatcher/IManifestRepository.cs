using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Dispatcher
{
    /// <summary>
    /// Repository interface for loading and querying GitLab group repo manifests.
    /// </summary>
    public interface IManifestRepository
    {
        /// <summary>
        /// Finds a manifest by matching an issue label/tag.
        /// Returns null if no manifest matches the given tag.
        /// </summary>
        /// <param name="issueTag">The label/tag to match</param>
        /// <returns>The matching manifest or null</returns>
        GitLabGroupRepoManifest FindByIssueTag(string issueTag);

        /// <summary>
        /// Gets all available manifests.
        /// </summary>
        /// <returns>Array of all manifests</returns>
        GitLabGroupRepoManifest[] GetAll();
    }
}
