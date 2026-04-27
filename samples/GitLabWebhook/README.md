# GitLab Issue Webhook Event Model

This sample demonstrates how to define a typed internal model for GitLab issue webhook events, along with parsing and validation logic that converts raw GitLab webhook payloads into an internal model.

## Overview

This implementation provides:

1. **Typed Internal Model**: A strongly-typed C# model (`GitLabIssueWebhookEvent`) that captures the minimum fields needed for dispatching GitLab issue webhook events.
2. **Parsing Logic**: Deserialization from raw JSON webhook payloads into the internal model.
3. **Validation**: Comprehensive validation ensuring all required fields are present and non-empty.
4. **Error Handling**: Typed validation errors with clear, actionable error messages.
5. **Graceful Handling**: Optional fields (labels, assignees, description) default to empty/zero values when absent.

## Model Structure

### GitLabIssueWebhookEvent

The main event model includes:

| Field | Type | Source in GitLab Payload | Required |
|-------|------|--------------------------|----------|
| `ProjectId` | `long` | `project.id` | Yes |
| `ProjectPath` | `string` | `project.path_with_namespace` | Yes |
| `IssueIid` | `long` | `object_attributes.iid` | Yes |
| `Title` | `string` | `object_attributes.title` | Yes |
| `Description` | `string` | `object_attributes.description` | No |
| `Labels` | `List<string>` | `labels[].title` | No |
| `Author` | `string` | `user.username` | Yes |
| `Assignees` | `List<string>` | `assignees[].username` | No |
| `State` | `string` | `object_attributes.state` | Yes |
| `WebUrl` | `string` | `object_attributes.url` | Yes |
| `Action` | `string` | `object_attributes.action` | Yes |
| `Timestamp` | `DateTimeOffset` | `object_attributes.created_at` / `updated_at` | Yes |

### ValidationError

Represents a field-specific validation error:

- `Field`: The name of the field that failed validation
- `Reason`: Human-readable description of the validation failure
- `ErrorCode`: Machine-readable error code (e.g., `VALIDATION_ERROR`, `DESERIALIZATION_ERROR`)

### ParseResult

Contains the result of parsing and validation:

- `Success`: `true` if parsing and validation succeeded, `false` otherwise
- `Event`: The parsed `GitLabIssueWebhookEvent` (null if validation failed)
- `Errors`: List of `ValidationError` objects (empty if successful)

## Usage

### Basic Parsing

```csharp
using GitLabWebhook.Parser;
using GitLabWebhook.Models;

var parser = new GitLabIssueWebhookParser();
var result = parser.Parse(webhookJsonPayload);

if (result.Success)
{
    var evt = result.Event;
    Console.WriteLine($"Issue #{evt.IssueIid}: {evt.Title}");
    Console.WriteLine($"Project: {evt.ProjectPath}");
    Console.WriteLine($"Action: {evt.Action}");
    Console.WriteLine($"Author: {evt.Author}");

    if (evt.Labels.Count > 0)
    {
        Console.WriteLine($"Labels: {string.Join(", ", evt.Labels)}");
    }

    if (evt.Assignees.Count > 0)
    {
        Console.WriteLine($"Assignees: {string.Join(", ", evt.Assignees)}");
    }
}
else
{
    Console.WriteLine("Validation failed:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error.Field}: {error.Reason} ({error.ErrorCode})");
    }
}
```

### Parsing from Byte Array

The parser also supports parsing from byte arrays (e.g., from HTTP request bodies):

```csharp
var parser = new GitLabIssueWebhookParser();
var result = parser.Parse(webhookPayloadBytes);
```

## Validation

### Required Fields

The parser validates that the following fields are present and non-empty:

- Project ID (must be non-zero)
- Project path
- Issue IID (must be non-zero)
- Issue title
- Web URL
- Action/event type
- State
- Author username
- Timestamp (either `created_at` or `updated_at`)

### Optional Fields

The following fields are optional and default to empty values when absent:

- Description (defaults to empty string)
- Labels (defaults to empty list)
- Assignees (defaults to empty list)

### Error Codes

The parser uses the following error codes:

- `VALIDATION_ERROR`: A required field is missing or empty
- `DESERIALIZATION_ERROR`: The JSON payload is malformed or cannot be deserialized

## Testing

The project includes comprehensive unit tests covering:

1. **Valid payloads**: Full payload, empty optional fields, missing optional fields
2. **Missing required fields**: Each required field tested individually
3. **Invalid payloads**: Null, empty, malformed JSON
4. **Field mappings**: Verification that GitLab webhook fields map correctly to the internal model
5. **Timestamp handling**: Both `created_at` and `updated_at` scenarios
6. **Byte array parsing**: Testing both string and byte array input methods

### Running Tests

```bash
dotnet test
```

## GitLab Issue Events Webhook Reference

This implementation follows the [GitLab Issues API webhook payload](https://docs.gitlab.com/ee/user/project/integrations/webhook_events.html#issue-events) format.

Example webhook payload structure:

```json
{
  "object_kind": "issue",
  "project": {
    "id": 123,
    "path_with_namespace": "mygroup/myproject"
  },
  "object_attributes": {
    "iid": 456,
    "title": "Issue title",
    "description": "Issue description",
    "state": "opened",
    "url": "https://gitlab.example.com/mygroup/myproject/-/issues/456",
    "action": "open",
    "created_at": "2024-01-15T10:30:00Z",
    "updated_at": "2024-01-15T10:30:00Z"
  },
  "user": {
    "username": "author-username"
  },
  "labels": [
    { "title": "bug" },
    { "title": "urgent" }
  ],
  "assignees": [
    { "username": "assignee1" },
    { "username": "assignee2" }
  ]
}
```

## Dependencies

- **Newtonsoft.Json 13.0.3**: JSON serialization/deserialization
- **MSTest**: Testing framework (for unit tests)

See `packages.txt` for the complete list of dependencies.

## Design Principles

This implementation follows patterns established in this repository:

1. **Simple POCOs**: No complex attributes or framework-specific annotations
2. **Clear validation**: Field-specific error messages with machine-readable error codes
3. **Defensive parsing**: Graceful handling of missing or null values
4. **Testability**: Pure business logic separated from infrastructure concerns
5. **Documentation**: Comprehensive XML documentation and README

## Acceptance Criteria

- [x] A typed internal model exists with all fields listed above
- [x] Parsing converts a raw GitLab webhook JSON payload into the model without panics or unhandled exceptions
- [x] Required-field validation returns a clear error when any required field is absent
- [x] Optional fields (labels, assignees, description) default to empty/zero values when absent
- [x] Tests cover: full valid payload, missing required fields, empty optional fields, and the field mappings expected by the GitLab Issue Events webhook format
