using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitLabWebhook.Models;
using GitLabWebhook.Parser;

namespace GitLabWebhook.Tests
{
    [TestClass]
    public class GitLabIssueWebhookParserTests
    {
        private GitLabIssueWebhookParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _parser = new GitLabIssueWebhookParser();
        }

        #region Valid Payload Tests

        [TestMethod]
        public void Parse_ValidFullPayload_ReturnsSuccess()
        {
            // Arrange
            string validPayload = @"{
                ""object_kind"": ""issue"",
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""description"": ""This is a test issue description."",
                    ""state"": ""opened"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z"",
                    ""updated_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                },
                ""labels"": [
                    { ""title"": ""bug"" },
                    { ""title"": ""urgent"" }
                ],
                ""assignees"": [
                    { ""username"": ""assignee1"" },
                    { ""username"": ""assignee2"" }
                ]
            }";

            // Act
            var result = _parser.Parse(validPayload);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success=true");
            Assert.IsNotNull(result.Event, "Expected non-null Event");
            Assert.AreEqual(0, result.Errors.Count, "Expected no validation errors");

            // Verify all fields
            Assert.AreEqual(123L, result.Event.ProjectId);
            Assert.AreEqual("mygroup/myproject", result.Event.ProjectPath);
            Assert.AreEqual(456L, result.Event.IssueIid);
            Assert.AreEqual("Test Issue", result.Event.Title);
            Assert.AreEqual("This is a test issue description.", result.Event.Description);
            Assert.AreEqual("opened", result.Event.State);
            Assert.AreEqual("https://gitlab.example.com/mygroup/myproject/-/issues/456", result.Event.WebUrl);
            Assert.AreEqual("open", result.Event.Action);
            Assert.AreEqual("testuser", result.Event.Author);
            Assert.AreEqual(2, result.Event.Labels.Count);
            Assert.IsTrue(result.Event.Labels.Contains("bug"));
            Assert.IsTrue(result.Event.Labels.Contains("urgent"));
            Assert.AreEqual(2, result.Event.Assignees.Count);
            Assert.IsTrue(result.Event.Assignees.Contains("assignee1"));
            Assert.IsTrue(result.Event.Assignees.Contains("assignee2"));
        }

        [TestMethod]
        public void Parse_ValidPayloadWithEmptyOptionalFields_ReturnsSuccess()
        {
            // Arrange
            string validPayload = @"{
                ""object_kind"": ""issue"",
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""description"": """",
                    ""state"": ""opened"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""updated_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(validPayload);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success=true");
            Assert.IsNotNull(result.Event, "Expected non-null Event");
            Assert.AreEqual(string.Empty, result.Event.Description, "Expected empty description");
            Assert.AreEqual(0, result.Event.Labels.Count, "Expected empty labels list");
            Assert.AreEqual(0, result.Event.Assignees.Count, "Expected empty assignees list");
        }

        [TestMethod]
        public void Parse_ValidPayloadWithMissingOptionalFields_ReturnsSuccess()
        {
            // Arrange
            string validPayload = @"{
                ""object_kind"": ""issue"",
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""state"": ""opened"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(validPayload);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success=true");
            Assert.IsNotNull(result.Event, "Expected non-null Event");
            Assert.AreEqual(string.Empty, result.Event.Description, "Expected empty description when missing");
            Assert.AreEqual(0, result.Event.Labels.Count, "Expected empty labels list when missing");
            Assert.AreEqual(0, result.Event.Assignees.Count, "Expected empty assignees list when missing");
        }

        [TestMethod]
        public void Parse_ValidPayloadWithUpdatedAt_UsesUpdatedAtAsTimestamp()
        {
            // Arrange
            string validPayload = @"{
                ""object_kind"": ""issue"",
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""state"": ""opened"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""update"",
                    ""created_at"": ""2024-01-15T10:30:00Z"",
                    ""updated_at"": ""2024-01-15T11:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(validPayload);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success=true");
            Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 11, 30, 0, TimeSpan.Zero), result.Event.Timestamp);
        }

        [TestMethod]
        public void Parse_ValidPayloadWithOnlyCreatedAt_UsesCreatedAtAsTimestamp()
        {
            // Arrange
            string validPayload = @"{
                ""object_kind"": ""issue"",
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""state"": ""opened"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(validPayload);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success=true");
            Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero), result.Event.Timestamp);
        }

        #endregion

        #region Missing Required Fields Tests

        [TestMethod]
        public void Parse_NullPayload_ReturnsValidationError()
        {
            // Act
            var result = _parser.Parse((string)null);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsNull(result.Event, "Expected null Event");
            Assert.AreEqual(1, result.Errors.Count, "Expected 1 validation error");
            Assert.AreEqual("payload", result.Errors[0].Field);
            Assert.AreEqual("VALIDATION_ERROR", result.Errors[0].ErrorCode);
        }

        [TestMethod]
        public void Parse_EmptyPayload_ReturnsValidationError()
        {
            // Act
            var result = _parser.Parse("");

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsNull(result.Event, "Expected null Event");
            Assert.AreEqual(1, result.Errors.Count, "Expected 1 validation error");
            Assert.AreEqual("payload", result.Errors[0].Field);
        }

        [TestMethod]
        public void Parse_InvalidJson_ReturnsDeserializationError()
        {
            // Arrange
            string invalidPayload = "{ invalid json }";

            // Act
            var result = _parser.Parse(invalidPayload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsNull(result.Event, "Expected null Event");
            Assert.AreEqual(1, result.Errors.Count, "Expected 1 error");
            Assert.AreEqual("payload", result.Errors[0].Field);
            Assert.AreEqual("DESERIALIZATION_ERROR", result.Errors[0].ErrorCode);
        }

        [TestMethod]
        public void Parse_MissingProjectId_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "project.id"), "Expected error for project.id");
        }

        [TestMethod]
        public void Parse_MissingProjectPath_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "project.path_with_namespace"), "Expected error for project.path_with_namespace");
        }

        [TestMethod]
        public void Parse_MissingIssueIid_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""title"": ""Test Issue"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "object_attributes.iid"), "Expected error for object_attributes.iid");
        }

        [TestMethod]
        public void Parse_MissingTitle_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "object_attributes.title"), "Expected error for object_attributes.title");
        }

        [TestMethod]
        public void Parse_MissingWebUrl_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "object_attributes.url"), "Expected error for object_attributes.url");
        }

        [TestMethod]
        public void Parse_MissingAction_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "object_attributes.action"), "Expected error for object_attributes.action");
        }

        [TestMethod]
        public void Parse_MissingTimestamp_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "object_attributes.timestamp"), "Expected error for timestamp");
        }

        [TestMethod]
        public void Parse_MissingAuthor_ReturnsValidationError()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Exists(e => e.Field == "user.username"), "Expected error for user.username");
        }

        [TestMethod]
        public void Parse_MultipleRequiredFieldsMissing_ReturnsMultipleValidationErrors()
        {
            // Arrange
            string payload = @"{
                ""project"": {
                    ""id"": 123
                },
                ""object_attributes"": {
                    ""iid"": 456
                }
            }";

            // Act
            var result = _parser.Parse(payload);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsTrue(result.Errors.Count > 1, "Expected multiple validation errors");
        }

        #endregion

        #region Byte Array Tests

        [TestMethod]
        public void Parse_ByteArray_ValidPayload_ReturnsSuccess()
        {
            // Arrange
            string validPayload = @"{
                ""project"": {
                    ""id"": 123,
                    ""path_with_namespace"": ""mygroup/myproject""
                },
                ""object_attributes"": {
                    ""iid"": 456,
                    ""title"": ""Test Issue"",
                    ""state"": ""opened"",
                    ""url"": ""https://gitlab.example.com/mygroup/myproject/-/issues/456"",
                    ""action"": ""open"",
                    ""created_at"": ""2024-01-15T10:30:00Z""
                },
                ""user"": {
                    ""username"": ""testuser""
                }
            }";
            byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(validPayload);

            // Act
            var result = _parser.Parse(payloadBytes);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success=true");
            Assert.IsNotNull(result.Event, "Expected non-null Event");
        }

        [TestMethod]
        public void Parse_ByteArray_NullPayload_ReturnsValidationError()
        {
            // Act
            var result = _parser.Parse((byte[])null);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsNull(result.Event, "Expected null Event");
            Assert.AreEqual(1, result.Errors.Count, "Expected 1 validation error");
        }

        [TestMethod]
        public void Parse_ByteArray_EmptyPayload_ReturnsValidationError()
        {
            // Act
            var result = _parser.Parse(new byte[0]);

            // Assert
            Assert.IsFalse(result.Success, "Expected Success=false");
            Assert.IsNull(result.Event, "Expected null Event");
            Assert.AreEqual(1, result.Errors.Count, "Expected 1 validation error");
        }

        #endregion

        #region Field Mapping Tests

        [TestMethod]
        public void Parse_ValidPayload_VerifiesCorrectFieldMapping()
        {
            // Arrange - Test that field mapping matches GitLab Issue Events webhook format
            string validPayload = @"{
                ""object_kind"": ""issue"",
                ""project"": {
                    ""id"": 9876,
                    ""path_with_namespace"": ""test-org/test-repo""
                },
                ""object_attributes"": {
                    ""iid"": 5432,
                    ""title"": ""Sample Issue Title"",
                    ""description"": ""Sample description text"",
                    ""state"": ""closed"",
                    ""url"": ""https://gitlab.example.com/test-org/test-repo/-/issues/5432"",
                    ""action"": ""close"",
                    ""created_at"": ""2024-01-01T00:00:00Z"",
                    ""updated_at"": ""2024-01-02T12:00:00Z""
                },
                ""user"": {
                    ""username"": ""author-user""
                },
                ""labels"": [
                    { ""title"": ""enhancement"" },
                    { ""title"": ""documentation"" },
                    { ""title"": ""good first issue"" }
                ],
                ""assignees"": [
                    { ""username"": ""dev1"" },
                    { ""username"": ""dev2"" },
                    { ""username"": ""dev3"" }
                ]
            }";

            // Act
            var result = _parser.Parse(validPayload);

            // Assert - Verify each mapping from spec
            Assert.IsTrue(result.Success);
            var evt = result.Event;

            // Project ID: project.id → ProjectId
            Assert.AreEqual(9876L, evt.ProjectId);

            // Project path: project.path_with_namespace → ProjectPath
            Assert.AreEqual("test-org/test-repo", evt.ProjectPath);

            // Issue IID: object_attributes.iid → IssueIid
            Assert.AreEqual(5432L, evt.IssueIid);

            // Title: object_attributes.title → Title
            Assert.AreEqual("Sample Issue Title", evt.Title);

            // Description: object_attributes.description → Description
            Assert.AreEqual("Sample description text", evt.Description);

            // State: object_attributes.state → State
            Assert.AreEqual("closed", evt.State);

            // Web URL: object_attributes.url → WebUrl
            Assert.AreEqual("https://gitlab.example.com/test-org/test-repo/-/issues/5432", evt.WebUrl);

            // Action: object_attributes.action → Action
            Assert.AreEqual("close", evt.Action);

            // Author: user.username → Author
            Assert.AreEqual("author-user", evt.Author);

            // Labels: labels[].title → Labels
            Assert.AreEqual(3, evt.Labels.Count);
            CollectionAssert.Contains(evt.Labels, "enhancement");
            CollectionAssert.Contains(evt.Labels, "documentation");
            CollectionAssert.Contains(evt.Labels, "good first issue");

            // Assignees: assignees[].username → Assignees
            Assert.AreEqual(3, evt.Assignees.Count);
            CollectionAssert.Contains(evt.Assignees, "dev1");
            CollectionAssert.Contains(evt.Assignees, "dev2");
            CollectionAssert.Contains(evt.Assignees, "dev3");

            // Timestamp: object_attributes.updated_at (preferred) → Timestamp
            Assert.AreEqual(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero), evt.Timestamp);
        }

        #endregion
    }
}
