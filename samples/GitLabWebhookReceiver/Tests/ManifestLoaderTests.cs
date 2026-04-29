using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitLabWebhookReceiver.Models;
using GitLabWebhookReceiver.Dispatcher;

namespace GitLabWebhookReceiver.Tests
{
    [TestClass]
    public class ManifestLoaderTests
    {
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "ManifestLoaderTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public void ManifestLoader_LoadsValidJsonFile()
        {
            var json = @"[
  {
    ""GitLabIssueTag"": ""backend"",
    ""GitLabTargetRepoUrl"": ""https://github.com/example/backend"",
    ""CodeRepositoryPath"": ""example/backend"",
    ""TargetRepoRef"": ""main""
  },
  {
    ""GitLabIssueTag"": ""frontend"",
    ""GitLabTargetRepoUrl"": ""https://github.com/example/frontend"",
    ""CodeRepositoryPath"": ""example/frontend"",
    ""TargetRepoRef"": """"
  }
]";
            var filePath = Path.Combine(_testDirectory, "manifests.json");
            File.WriteAllText(filePath, json);

            var manifests = ManifestLoader.LoadFromJsonFile(filePath);

            Assert.IsNotNull(manifests);
            Assert.AreEqual(2, manifests.Length);
            Assert.AreEqual("backend", manifests[0].GitLabIssueTag);
            Assert.AreEqual("https://github.com/example/backend", manifests[0].GitLabTargetRepoUrl);
            Assert.AreEqual("frontend", manifests[1].GitLabIssueTag);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ManifestLoader_ThrowsOnMissingFile()
        {
            var filePath = Path.Combine(_testDirectory, "nonexistent.json");
            ManifestLoader.LoadFromJsonFile(filePath);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ManifestLoader_ThrowsOnInvalidManifest()
        {
            var json = @"[
  {
    ""GitLabIssueTag"": """",
    ""GitLabTargetRepoUrl"": ""https://github.com/example/backend"",
    ""CodeRepositoryPath"": ""example/backend""
  }
]";
            var filePath = Path.Combine(_testDirectory, "invalid.json");
            File.WriteAllText(filePath, json);

            ManifestLoader.LoadFromJsonFile(filePath);
        }

        [TestMethod]
        [ExpectedException(typeof(Newtonsoft.Json.JsonException))]
        public void ManifestLoader_ThrowsOnInvalidJson()
        {
            var json = "{ this is not valid json }";
            var filePath = Path.Combine(_testDirectory, "invalid.json");
            File.WriteAllText(filePath, json);

            ManifestLoader.LoadFromJsonFile(filePath);
        }

        [TestMethod]
        public void ManifestLoader_TryLoad_ReturnsNullOnError()
        {
            var filePath = Path.Combine(_testDirectory, "nonexistent.json");

            var manifests = ManifestLoader.TryLoadFromJsonFile(filePath, out var error);

            Assert.IsNull(manifests);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Contains("Failed to load manifests"));
        }

        [TestMethod]
        public void ManifestLoader_TryLoad_SucceedsOnValidFile()
        {
            var json = @"[
  {
    ""GitLabIssueTag"": ""backend"",
    ""GitLabTargetRepoUrl"": ""https://github.com/example/backend"",
    ""CodeRepositoryPath"": ""example/backend""
  }
]";
            var filePath = Path.Combine(_testDirectory, "manifests.json");
            File.WriteAllText(filePath, json);

            var manifests = ManifestLoader.TryLoadFromJsonFile(filePath, out var error);

            Assert.IsNotNull(manifests);
            Assert.IsNull(error);
            Assert.AreEqual(1, manifests.Length);
        }

        [TestMethod]
        public void ManifestLoader_LoadsEmptyArray()
        {
            var json = "[]";
            var filePath = Path.Combine(_testDirectory, "empty.json");
            File.WriteAllText(filePath, json);

            var manifests = ManifestLoader.LoadFromJsonFile(filePath);

            Assert.IsNotNull(manifests);
            Assert.AreEqual(0, manifests.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ManifestLoader_ThrowsOnEmptyPath()
        {
            ManifestLoader.LoadFromJsonFile("");
        }
    }
}
