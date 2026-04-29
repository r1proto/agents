# GitLab Issue Event Dispatcher Implementation

## Overview

This document describes the dispatcher service implementation that processes GitLab issue webhook events, performs idempotency checks, and submits tasks to the OpenCode AI Agent.

## Components

### 1. Internal Task Schema (`Models/AgentTask.cs`)

The `AgentTask` class represents a normalized, provider-agnostic task payload that separates the source project (where the issue is filed) from the target project (where code changes will be made).

**Source Fields** (GitLab issue metadata):
- `Source`: Fixed value "gitlab"
- `InstanceUrl`: Base URL of the GitLab instance
- `SourceProjectId`: Numeric ID of the GitLab project
- `SourceProjectPath`: Full path (e.g., "group/subgroup/project")
- `IssueIid`: Project-scoped issue identifier
- `Title`, `Description`, `Labels`, `Author`, `Assignees`, `State`
- `WebUrl`: Human-visible URL of the issue
- `EventAction`: Webhook action (open, update, etc.)
- `EventTimestamp`: ISO-8601 timestamp

**Target Fields** (codebase to work on):
- `TargetRepoUrl`: URL of repository for code changes (required)
- `TargetRepoRef`: Optional branch/ref (defaults to empty string)

**Key Method**:
```csharp
AgentTask.FromGitLabIssueEvent(
    GitLabIssueEvent issueEvent,
    string instanceUrl,
    string targetRepoUrl,
    string targetRepoRef = "")
```

### 2. Integration Configuration (`Config/WebhookConfig.cs`)

Extended configuration supporting GitLab integration (Issue #5):

**New Configuration Fields**:
- `GitLabBaseUrl`: Base URL of GitLab instance (must be https://)
- `TargetRepoUrl`: Target repository URL (required)
- `TargetRepoRef`: Optional default branch/ref
- `Enabled`: Boolean flag to enable integration (defaults to false)
- `DisplayName`: Optional human-readable label

**Validation**:
- `ValidateConfig()`: Returns null if valid, error message if invalid
- Validates URL format, https requirement, non-empty required fields

### 3. Agent Submission Service (`Dispatcher/IAgentSubmissionService.cs`)

Provider-agnostic interface for submitting tasks to the agent:

```csharp
public interface IAgentSubmissionService
{
    AgentSubmissionResult SubmitTask(AgentTask task);
}

public class AgentSubmissionResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string TaskId { get; set; }
}
```

**Stub Implementation**: `StubAgentSubmissionService` logs to console and returns success.

### 4. Dispatcher Service (`Dispatcher/GitLabIssueEventDispatcher.cs`)

The main dispatcher implementation with idempotency checking.

**Key Features**:

1. **Idempotency Check**:
   - Deduplication key: `SHA256(project_id + issue_iid + action + timestamp)`
   - Timestamp truncated to seconds for stability
   - In-memory cache with size limit (10,000 entries)
   - Production should use distributed cache (Redis) with TTL

2. **Event Processing**:
   - Validates input
   - Checks for duplicates
   - Converts to `AgentTask`
   - Submits to agent via `IAgentSubmissionService`
   - Returns structured result

3. **Error Handling**:
   - Failed submissions are removed from cache (allows retry)
   - Exceptions are caught and logged
   - Returns failure result without swallowing errors

**Result Types**:
- `Success`: Event dispatched successfully
- `Duplicate`: Event already processed (not an error)
- `Failure`: Submission failed

**Constructor**:
```csharp
public GitLabIssueEventDispatcher(
    IAgentSubmissionService agentSubmissionService,
    string gitlabBaseUrl,
    string targetRepoUrl,
    string targetRepoRef = "")
```

### 5. Tests (`Tests/DispatcherTests.cs`)

Comprehensive test coverage:

**AgentTask Tests**:
- Field population from GitLabIssueEvent
- Empty labels handling
- Default target ref
- Null/invalid input validation

**Dispatcher Tests**:
- Successful dispatch
- Duplicate detection (same event twice)
- Different events both dispatched
- Agent submission failure handling
- Failed submission allows retry
- Different actions generate different keys
- Different timestamps generate different keys
- Constructor validation

**Test Doubles**:
- `TestAgentSubmissionService`: Tracks calls and can simulate failures

## Integration with Webhook Receiver

The dispatcher is integrated in `WebhookReceiver/Program.cs`:

1. Validates configuration at startup
2. Creates dispatcher based on config:
   - If valid config and enabled: Uses `GitLabIssueEventDispatcher`
   - Otherwise: Uses `StubIssueEventDispatcher`
3. Passes dispatcher to `WebhookServer`

## Configuration Example

```xml
<appSettings>
  <add key="GitLab:WebhookSecret" value="your-secret-token" />
  <add key="GitLab:BaseUrl" value="https://gitlab.company.com" />
  <add key="GitLab:TargetRepoUrl" value="https://github.com/org/repo" />
  <add key="GitLab:TargetRepoRef" value="main" />
  <add key="GitLab:Enabled" value="true" />
  <add key="GitLab:DisplayName" value="Company GitLab" />
</appSettings>
```

## Design Decisions

### Separation of Concerns

- **GitLab-specific logic**: Confined to `GitLabIssueEventDispatcher` and `GitLabIssueEvent`
- **Provider-agnostic logic**: `AgentTask`, `IAgentSubmissionService`
- Agent submission layer has no knowledge of GitLab

### Idempotency Strategy

- Deduplication key includes all identifying factors: project, issue, action, timestamp
- Different actions on same issue generate different keys (intentional)
- Timestamp ensures updates at different times are processed separately
- Failed submissions don't count as processed (allows retry)

### In-Memory Cache Limitations

The current implementation uses an in-memory cache for simplicity. For production:

1. Replace with distributed cache (Redis, Memcached)
2. Add TTL expiration (e.g., 24 hours)
3. Consider persistent storage for audit trail
4. Implement proper LRU eviction

### Error Handling Philosophy

- Validation errors throw exceptions (fail fast)
- Submission errors return failure results (graceful)
- All errors are logged
- Failed submissions don't block future attempts

## Acceptance Criteria Met

- ✅ Dispatcher converts `GitLabIssueEvent` to internal task payload
- ✅ Submits to agent (via `IAgentSubmissionService` interface)
- ✅ Same event twice results in one submission, second returns duplicate
- ✅ Downstream agent failures returned as failure result (not silently ignored)
- ✅ Agent submission path contains no GitLab-specific code
- ✅ Tests cover: successful dispatch, duplicate detection, downstream failure
- ✅ Reuses repository patterns (config, test structure, error handling)

## Future Enhancements

1. **Distributed Caching**: Replace in-memory cache with Redis
2. **Audit Trail**: Log all dispatch attempts with outcomes
3. **Metrics**: Add counters for success/duplicate/failure
4. **Retry Logic**: Implement exponential backoff for failures
5. **Event Filtering**: Support filtering by labels or other criteria
6. **Async Processing**: Add async/await support for better scalability
7. **Real Agent Integration**: Replace `StubAgentSubmissionService` with actual agent API

## Related Issues

- **Issue #2**: GitLab issue event model (dependency - completed)
- **Issue #3**: Dispatcher service (this implementation)
- **Issue #4**: Internal task schema (dependency - completed)
- **Issue #5**: Integration configuration (dependency - completed)
- **Issue #7**: End-to-end wiring (next step)
