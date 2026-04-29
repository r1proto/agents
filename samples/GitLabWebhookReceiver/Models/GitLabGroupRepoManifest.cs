using System;

namespace GitLabWebhookReceiver.Models
{
    /// <summary>
    /// GitLab group repository manifest that defines target repository routing
    /// based on issue labels. This replaces app-config-based target repo lookup.
    /// </summary>
    public class GitLabGroupRepoManifest
    {
        /// <summary>
        /// The GitLab label/tag used to identify or route issues to this target repository.
        /// Example: "backend", "frontend", "infrastructure"
        /// </summary>
        public string GitLabIssueTag { get; set; }

        /// <summary>
        /// The canonical URL of the target GitLab or GitHub repository where code changes will be made.
        /// Example: "https://github.com/example/backend-service"
        /// </summary>
        public string GitLabTargetRepoUrl { get; set; }

        /// <summary>
        /// The path to the code repository used by the system.
        /// This can be a local path or additional routing information.
        /// Example: "example/backend-service" or "/repos/backend"
        /// </summary>
        public string CodeRepositoryPath { get; set; }

        /// <summary>
        /// Optional default branch/ref for the agent to start from.
        /// Defaults to empty string, which means use the repository's default branch.
        /// </summary>
        public string TargetRepoRef { get; set; }

        /// <summary>
        /// Validates that all required fields are populated and valid.
        /// </summary>
        /// <returns>Error message if invalid, null if valid</returns>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(GitLabIssueTag))
                return "GitLabIssueTag is required";

            if (string.IsNullOrWhiteSpace(GitLabTargetRepoUrl))
                return "GitLabTargetRepoUrl is required";

            if (!Uri.TryCreate(GitLabTargetRepoUrl, UriKind.Absolute, out _))
                return "GitLabTargetRepoUrl is not a valid URL";

            if (string.IsNullOrWhiteSpace(CodeRepositoryPath))
                return "CodeRepositoryPath is required";

            return null;
        }

        /// <summary>
        /// Creates a valid manifest instance for testing purposes.
        /// </summary>
        public static GitLabGroupRepoManifest CreateExample()
        {
            return new GitLabGroupRepoManifest
            {
                GitLabIssueTag = "backend",
                GitLabTargetRepoUrl = "https://github.com/example/backend-service",
                CodeRepositoryPath = "example/backend-service",
                TargetRepoRef = ""
            };
        }
    }
}
