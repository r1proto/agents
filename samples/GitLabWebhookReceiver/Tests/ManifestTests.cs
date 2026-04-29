using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitLabWebhookReceiver.Models;
using GitLabWebhookReceiver.Dispatcher;

namespace GitLabWebhookReceiver.Tests
{
    [TestClass]
    public class ManifestTests
    {
        // ===== GitLabGroupRepoManifest Tests =====

        [TestMethod]
        public void Manifest_Validate_AcceptsValidManifest()
        {
            var manifest = new GitLabGroupRepoManifest
            {
                GitLabIssueTag = "backend",
                GitLabTargetRepoUrl = "https://github.com/example/backend",
                CodeRepositoryPath = "example/backend"
            };

            var error = manifest.Validate();
            Assert.IsNull(error, "Valid manifest should not have validation errors");
        }

        [TestMethod]
        public void Manifest_Validate_RejectsMissingTag()
        {
            var manifest = new GitLabGroupRepoManifest
            {
                GitLabIssueTag = "",
                GitLabTargetRepoUrl = "https://github.com/example/backend",
                CodeRepositoryPath = "example/backend"
            };

            var error = manifest.Validate();
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Contains("GitLabIssueTag"));
        }

        [TestMethod]
        public void Manifest_Validate_RejectsMissingTargetRepoUrl()
        {
            var manifest = new GitLabGroupRepoManifest
            {
                GitLabIssueTag = "backend",
                GitLabTargetRepoUrl = "",
                CodeRepositoryPath = "example/backend"
            };

            var error = manifest.Validate();
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Contains("GitLabTargetRepoUrl"));
        }

        [TestMethod]
        public void Manifest_Validate_RejectsInvalidUrl()
        {
            var manifest = new GitLabGroupRepoManifest
            {
                GitLabIssueTag = "backend",
                GitLabTargetRepoUrl = "not-a-valid-url",
                CodeRepositoryPath = "example/backend"
            };

            var error = manifest.Validate();
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Contains("not a valid URL"));
        }

        [TestMethod]
        public void Manifest_Validate_RejectsMissingCodeRepositoryPath()
        {
            var manifest = new GitLabGroupRepoManifest
            {
                GitLabIssueTag = "backend",
                GitLabTargetRepoUrl = "https://github.com/example/backend",
                CodeRepositoryPath = ""
            };

            var error = manifest.Validate();
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Contains("CodeRepositoryPath"));
        }

        [TestMethod]
        public void Manifest_CreateExample_ReturnsValidManifest()
        {
            var manifest = GitLabGroupRepoManifest.CreateExample();

            Assert.IsNotNull(manifest);
            Assert.IsNull(manifest.Validate());
            Assert.AreEqual("backend", manifest.GitLabIssueTag);
            Assert.IsFalse(string.IsNullOrEmpty(manifest.GitLabTargetRepoUrl));
            Assert.IsFalse(string.IsNullOrEmpty(manifest.CodeRepositoryPath));
        }

        // ===== InMemoryManifestRepository Tests =====

        [TestMethod]
        public void ManifestRepository_FindByIssueTag_FindsExistingTag()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "backend",
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend"
                },
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "frontend",
                    GitLabTargetRepoUrl = "https://github.com/example/frontend",
                    CodeRepositoryPath = "example/frontend"
                }
            };

            var repo = new InMemoryManifestRepository(manifests);

            var result = repo.FindByIssueTag("backend");
            Assert.IsNotNull(result);
            Assert.AreEqual("backend", result.GitLabIssueTag);
            Assert.AreEqual("https://github.com/example/backend", result.GitLabTargetRepoUrl);
        }

        [TestMethod]
        public void ManifestRepository_FindByIssueTag_ReturnsNullForMissingTag()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "backend",
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend"
                }
            };

            var repo = new InMemoryManifestRepository(manifests);

            var result = repo.FindByIssueTag("nonexistent");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ManifestRepository_FindByIssueTag_IsCaseInsensitive()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "BackEnd",
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend"
                }
            };

            var repo = new InMemoryManifestRepository(manifests);

            var result1 = repo.FindByIssueTag("backend");
            var result2 = repo.FindByIssueTag("BACKEND");
            var result3 = repo.FindByIssueTag("BackEnd");

            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(result3);
            Assert.AreEqual(result1.GitLabTargetRepoUrl, result2.GitLabTargetRepoUrl);
            Assert.AreEqual(result2.GitLabTargetRepoUrl, result3.GitLabTargetRepoUrl);
        }

        [TestMethod]
        public void ManifestRepository_FindByIssueTag_ReturnsNullForEmptyTag()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "backend",
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend"
                }
            };

            var repo = new InMemoryManifestRepository(manifests);

            Assert.IsNull(repo.FindByIssueTag(""));
            Assert.IsNull(repo.FindByIssueTag(null));
            Assert.IsNull(repo.FindByIssueTag("   "));
        }

        [TestMethod]
        public void ManifestRepository_GetAll_ReturnsAllManifests()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "backend",
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend"
                },
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "frontend",
                    GitLabTargetRepoUrl = "https://github.com/example/frontend",
                    CodeRepositoryPath = "example/frontend"
                }
            };

            var repo = new InMemoryManifestRepository(manifests);

            var all = repo.GetAll();
            Assert.AreEqual(2, all.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ManifestRepository_Constructor_ThrowsOnNullManifests()
        {
            new InMemoryManifestRepository(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ManifestRepository_Constructor_ThrowsOnInvalidManifest()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "", // Invalid
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend"
                }
            };

            new InMemoryManifestRepository(manifests);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ManifestRepository_Constructor_ThrowsOnDuplicateTags()
        {
            var manifests = new[]
            {
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "backend",
                    GitLabTargetRepoUrl = "https://github.com/example/backend",
                    CodeRepositoryPath = "example/backend"
                },
                new GitLabGroupRepoManifest
                {
                    GitLabIssueTag = "backend", // Duplicate
                    GitLabTargetRepoUrl = "https://github.com/example/backend2",
                    CodeRepositoryPath = "example/backend2"
                }
            };

            new InMemoryManifestRepository(manifests);
        }

        [TestMethod]
        public void ManifestRepository_AcceptsEmptyManifestArray()
        {
            var repo = new InMemoryManifestRepository(new GitLabGroupRepoManifest[0]);
            var all = repo.GetAll();
            Assert.AreEqual(0, all.Length);
            Assert.IsNull(repo.FindByIssueTag("anything"));
        }
    }
}
