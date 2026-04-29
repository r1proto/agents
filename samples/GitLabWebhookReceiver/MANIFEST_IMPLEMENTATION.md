# GitLab Group Repo Manifest Implementation

## Overview

This document describes the GitLab group repo manifest feature that replaces app-config-based target repository lookup with a manifest-driven approach. This allows flexible, label-based routing of GitLab issues to different code repositories.

## Architecture

### Components

1. **GitLabGroupRepoManifest** (`Models/GitLabGroupRepoManifest.cs`)
   - Data model representing a manifest entry
   - Contains: GitLabIssueTag, GitLabTargetRepoUrl, CodeRepositoryPath, TargetRepoRef
   - Includes validation logic

2. **IManifestRepository** (`Dispatcher/IManifestRepository.cs`)
   - Interface for querying manifests
   - Supports finding manifests by issue tag and listing all manifests

3. **InMemoryManifestRepository** (`Dispatcher/InMemoryManifestRepository.cs`)
   - In-memory implementation of IManifestRepository
   - Validates manifests on construction
   - Provides case-insensitive tag matching

4. **ManifestLoader** (`Dispatcher/ManifestLoader.cs`)
   - Utility for loading manifests from JSON files
   - Validates manifests during load
   - Provides both throwing and try-pattern methods

5. **ManifestDrivenGitLabDispatcher** (`Dispatcher/ManifestDrivenGitLabDispatcher.cs`)
   - Dispatcher that uses manifests to resolve target repositories
   - Routes issues to repositories based on issue labels
   - Maintains backward compatibility with existing dispatcher interface

## Manifest Schema

### JSON Format

```json
[
  {
    "GitLabIssueTag": "backend",
    "GitLabTargetRepoUrl": "https://github.com/example-org/backend-service",
    "CodeRepositoryPath": "example-org/backend-service",
    "TargetRepoRef": "main"
  },
  {
    "GitLabIssueTag": "frontend",
    "GitLabTargetRepoUrl": "https://github.com/example-org/frontend-app",
    "CodeRepositoryPath": "example-org/frontend-app",
    "TargetRepoRef": "develop"
  }
]
```

### Field Descriptions

- **GitLabIssueTag** (required): The GitLab label used to identify issues for this target repository
- **GitLabTargetRepoUrl** (required): The URL of the target repository where code changes will be made
- **CodeRepositoryPath** (required): The path to the code repository used by the system
- **TargetRepoRef** (optional): Default branch/ref for the agent to start from (empty = use repo default)

## How It Works

### Routing Logic

1. When a GitLab issue event is received, the dispatcher extracts all labels from the issue
2. For each label, the dispatcher queries the manifest repository
3. The first matching manifest determines the target repository
4. If no manifest matches any label, the dispatch fails with a clear error message

### Example

Given these manifests:
```json
[
  {"GitLabIssueTag": "backend", "GitLabTargetRepoUrl": "https://github.com/org/backend", ...},
  {"GitLabIssueTag": "frontend", "GitLabTargetRepoUrl": "https://github.com/org/frontend", ...}
]
```

- Issue with labels `["backend", "bug"]` → routes to `org/backend`
- Issue with labels `["frontend", "feature"]` → routes to `org/frontend`
- Issue with labels `["bug", "feature"]` → fails (no matching manifest)

## Usage

### Basic Setup

```csharp
// Load manifests from JSON file
var manifests = ManifestLoader.LoadFromJsonFile("manifests.json");

// Create manifest repository
var manifestRepo = new InMemoryManifestRepository(manifests);

// Create dispatcher
var agentService = new YourAgentSubmissionService();
var dispatcher = new ManifestDrivenGitLabDispatcher(
    agentService,
    manifestRepo,
    "https://gitlab.example.com");

// Dispatch issue events
dispatcher.DispatchIssueEvent(gitlabIssueEvent);
```

### Error Handling

```csharp
// Try-pattern for loading manifests
var manifests = ManifestLoader.TryLoadFromJsonFile("manifests.json", out var error);
if (manifests == null)
{
    Console.WriteLine($"Failed to load manifests: {error}");
    return;
}
```

## Migration from App-Config

### Old Approach (App.config)

```csharp
// Target repo was hardcoded in configuration
var targetRepoUrl = WebhookConfig.TargetRepoUrl;
var dispatcher = new GitLabIssueEventDispatcher(
    agentService,
    gitlabBaseUrl,
    targetRepoUrl,
    targetRepoRef);
```

### New Approach (Manifest-Driven)

```csharp
// Target repo is resolved from manifests based on issue labels
var manifests = ManifestLoader.LoadFromJsonFile("manifests.json");
var manifestRepo = new InMemoryManifestRepository(manifests);
var dispatcher = new ManifestDrivenGitLabDispatcher(
    agentService,
    manifestRepo,
    gitlabBaseUrl);
```

### Benefits

1. **Flexible Routing**: Route different issues to different repositories based on labels
2. **No Code Changes**: Update routing by modifying manifest file
3. **Clear Errors**: Explicit error messages when no manifest matches
4. **Centralized Configuration**: All routing rules in one place
5. **GitLab Group Management**: Manifests can be managed at the GitLab group level

## Testing

Comprehensive tests cover:

- **Manifest validation**: Required fields, valid URLs, etc.
- **Repository queries**: Finding by tag, case-insensitivity, missing tags
- **Dispatcher routing**: Correct repo selection, multiple labels, missing manifests
- **Idempotency**: Duplicate detection and retry after failure
- **Manifest loading**: JSON parsing, file errors, validation errors

Run tests:
```bash
# Using .NET CLI (if available)
dotnet test

# Or using MSTest directly
mstest /testcontainer:bin/tests/GitLabWebhookReceiver.Tests.dll
```

## Configuration Example

See `Config/manifests.example.json` for a complete example with multiple repository targets.

## Future Enhancements

1. **Dynamic Loading**: Load manifests from GitLab API or database
2. **Priority/Ordering**: Support for manifest priority when multiple labels match
3. **Pattern Matching**: Support wildcards or regex in issue tags
4. **Caching**: Add distributed caching for manifest lookups
5. **Metrics**: Track which manifests are used most frequently
