using System;
using System.IO;
using Newtonsoft.Json;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Dispatcher
{
    /// <summary>
    /// Loads GitLab group repo manifests from a JSON file.
    /// </summary>
    public static class ManifestLoader
    {
        /// <summary>
        /// Loads manifests from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON manifest file</param>
        /// <returns>Array of manifests</returns>
        /// <exception cref="FileNotFoundException">If the file doesn't exist</exception>
        /// <exception cref="JsonException">If the JSON is invalid</exception>
        /// <exception cref="ArgumentException">If any manifest is invalid</exception>
        public static GitLabGroupRepoManifest[] LoadFromJsonFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Manifest file not found: {filePath}");

            var json = File.ReadAllText(filePath);
            var manifests = JsonConvert.DeserializeObject<GitLabGroupRepoManifest[]>(json);

            if (manifests == null)
                throw new JsonException("Failed to deserialize manifests from JSON");

            // Validate all manifests
            for (int i = 0; i < manifests.Length; i++)
            {
                var manifest = manifests[i];
                var validationError = manifest?.Validate();
                if (validationError != null)
                    throw new ArgumentException($"Manifest at index {i} is invalid: {validationError}");
            }

            return manifests;
        }

        /// <summary>
        /// Tries to load manifests from a JSON file, returning null on error.
        /// </summary>
        /// <param name="filePath">Path to the JSON manifest file</param>
        /// <param name="error">Error message if loading failed</param>
        /// <returns>Array of manifests or null on error</returns>
        public static GitLabGroupRepoManifest[] TryLoadFromJsonFile(string filePath, out string error)
        {
            try
            {
                var manifests = LoadFromJsonFile(filePath);
                error = null;
                return manifests;
            }
            catch (Exception ex)
            {
                error = $"Failed to load manifests: {ex.Message}";
                return null;
            }
        }
    }
}
