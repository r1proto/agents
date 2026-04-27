using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitLabWebhook.Models;
using Newtonsoft.Json;

namespace GitLabWebhook.Parser
{
    /// <summary>
    /// Parser for GitLab issue webhook payloads.
    /// Deserializes JSON payloads and validates required fields.
    /// </summary>
    public class GitLabIssueWebhookParser
    {
        /// <summary>
        /// Parses a raw GitLab webhook JSON payload into a validated internal event model.
        /// </summary>
        /// <param name="jsonPayload">The raw JSON payload string from GitLab</param>
        /// <returns>A ParseResult containing either the parsed event or validation errors</returns>
        public ParseResult Parse(string jsonPayload)
        {
            var errors = new List<ValidationError>();

            // Check for null or empty payload
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                errors.Add(new ValidationError("payload", "Payload cannot be null or empty.", "VALIDATION_ERROR"));
                return ParseResult.FailureResult(errors);
            }

            GitLabWebhookPayload payload;
            try
            {
                payload = JsonConvert.DeserializeObject<GitLabWebhookPayload>(jsonPayload);
            }
            catch (JsonException ex)
            {
                errors.Add(new ValidationError("payload", $"Invalid JSON format: {ex.Message}", "DESERIALIZATION_ERROR"));
                return ParseResult.FailureResult(errors);
            }

            if (payload == null)
            {
                errors.Add(new ValidationError("payload", "Deserialized payload is null.", "DESERIALIZATION_ERROR"));
                return ParseResult.FailureResult(errors);
            }

            // Validate required fields and build the event model
            var evt = new GitLabIssueWebhookEvent();

            // Validate project
            if (payload.project == null)
            {
                errors.Add(new ValidationError("project", "Project information is required.", "VALIDATION_ERROR"));
            }
            else
            {
                if (payload.project.id == 0)
                {
                    errors.Add(new ValidationError("project.id", "Project ID is required and must be non-zero.", "VALIDATION_ERROR"));
                }
                else
                {
                    evt.ProjectId = payload.project.id;
                }

                if (string.IsNullOrWhiteSpace(payload.project.path_with_namespace))
                {
                    errors.Add(new ValidationError("project.path_with_namespace", "Project path is required.", "VALIDATION_ERROR"));
                }
                else
                {
                    evt.ProjectPath = payload.project.path_with_namespace;
                }
            }

            // Validate object_attributes
            if (payload.object_attributes == null)
            {
                errors.Add(new ValidationError("object_attributes", "Object attributes are required.", "VALIDATION_ERROR"));
            }
            else
            {
                if (payload.object_attributes.iid == 0)
                {
                    errors.Add(new ValidationError("object_attributes.iid", "Issue IID is required and must be non-zero.", "VALIDATION_ERROR"));
                }
                else
                {
                    evt.IssueIid = payload.object_attributes.iid;
                }

                if (string.IsNullOrWhiteSpace(payload.object_attributes.title))
                {
                    errors.Add(new ValidationError("object_attributes.title", "Issue title is required.", "VALIDATION_ERROR"));
                }
                else
                {
                    evt.Title = payload.object_attributes.title;
                }

                if (string.IsNullOrWhiteSpace(payload.object_attributes.url))
                {
                    errors.Add(new ValidationError("object_attributes.url", "Issue web URL is required.", "VALIDATION_ERROR"));
                }
                else
                {
                    evt.WebUrl = payload.object_attributes.url;
                }

                if (string.IsNullOrWhiteSpace(payload.object_attributes.action))
                {
                    errors.Add(new ValidationError("object_attributes.action", "Action/event type is required.", "VALIDATION_ERROR"));
                }
                else
                {
                    evt.Action = payload.object_attributes.action;
                }

                // Optional: description
                evt.Description = payload.object_attributes.description ?? string.Empty;

                // Required: state
                if (string.IsNullOrWhiteSpace(payload.object_attributes.state))
                {
                    errors.Add(new ValidationError("object_attributes.state", "Issue state is required.", "VALIDATION_ERROR"));
                }
                else
                {
                    evt.State = payload.object_attributes.state;
                }

                // Parse timestamp (prefer updated_at, fall back to created_at)
                string timestampStr = payload.object_attributes.updated_at ?? payload.object_attributes.created_at;
                if (string.IsNullOrWhiteSpace(timestampStr))
                {
                    errors.Add(new ValidationError("object_attributes.timestamp", "Timestamp (created_at or updated_at) is required.", "VALIDATION_ERROR"));
                }
                else
                {
                    if (DateTime.TryParse(timestampStr, out DateTime timestamp))
                    {
                        evt.Timestamp = timestamp;
                    }
                    else
                    {
                        errors.Add(new ValidationError("object_attributes.timestamp", "Invalid timestamp format.", "VALIDATION_ERROR"));
                    }
                }
            }

            // Validate user (author)
            if (payload.user == null || string.IsNullOrWhiteSpace(payload.user.username))
            {
                errors.Add(new ValidationError("user.username", "Author username is required.", "VALIDATION_ERROR"));
            }
            else
            {
                evt.Author = payload.user.username;
            }

            // Optional: labels
            if (payload.labels != null && payload.labels.Count > 0)
            {
                evt.Labels = payload.labels
                    .Where(l => l != null && !string.IsNullOrWhiteSpace(l.title))
                    .Select(l => l.title)
                    .ToList();
            }
            else
            {
                evt.Labels = new List<string>();
            }

            // Optional: assignees
            if (payload.assignees != null && payload.assignees.Count > 0)
            {
                evt.Assignees = payload.assignees
                    .Where(a => a != null && !string.IsNullOrWhiteSpace(a.username))
                    .Select(a => a.username)
                    .ToList();
            }
            else
            {
                evt.Assignees = new List<string>();
            }

            // Return success or failure based on validation errors
            if (errors.Count > 0)
            {
                return ParseResult.FailureResult(errors);
            }

            return ParseResult.SuccessResult(evt);
        }

        /// <summary>
        /// Parses a raw GitLab webhook byte array into a validated internal event model.
        /// </summary>
        /// <param name="payload">The raw byte array from GitLab</param>
        /// <returns>A ParseResult containing either the parsed event or validation errors</returns>
        public ParseResult Parse(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                var errors = new List<ValidationError>
                {
                    new ValidationError("payload", "Payload cannot be null or empty.", "VALIDATION_ERROR")
                };
                return ParseResult.FailureResult(errors);
            }

            string jsonPayload = Encoding.UTF8.GetString(payload);
            return Parse(jsonPayload);
        }
    }
}
