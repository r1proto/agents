# AgentSubmissionService Implementation

This document describes the implementation of the AgentSubmissionService for integrating GitLab webhook events with OpenCode AI agents.

## Overview

The AgentSubmissionService provides a complete pipeline for dispatching GitLab issue events to OpenCode AI agents, enabling automated issue resolution and task management.

## Architecture

The implementation follows a clean, layered architecture:

```
GitLab Webhook
    ↓
WebhookReceiver (validates & parses)
    ↓
IIssueEventDispatcher (AgentIssueEventDispatcher)
    ↓
AgentTask (internal schema)
    ↓
IAgentSubmissionService (OpenCodeAgentSubmissionService)
    ↓
OpenCode AI Agent
```

## Components

### 1. AgentTask (Internal Task Schema)

**Location**: `Models/AgentTask.cs`

A provider-agnostic schema that represents a task to be submitted to the agent. It separates:
- **Source fields**: GitLab metadata (project, issue, labels, etc.)
- **Target fields**: Where the agent should work (repository URL, branch)

**Key Features**:
- Converts GitLabIssueEvent to a normalized format
- Generates deduplication keys for idempotency
- Validates required fields

**Example**:
```csharp
var task = AgentTask.FromGitLabEvent(issueEvent, config);
var dedupKey = task.GetDeduplicationKey();
```

### 2. GitLabIntegrationConfig

**Location**: `Models/GitLabIntegrationConfig.cs`

IT-managed configuration for the GitLab integration. Stores:
- GitLab instance base URL (HTTPS required)
- Webhook secret for validation
- Target repository URL where agents work
- Optional target branch/ref
- Enable/disable flag

**Key Features**:
- Validates URLs and security requirements
- Normalizes trailing slashes
- Provides sanitized output (masks secrets)

**Example**:
```csharp
var config = WebhookConfig.GetIntegrationConfig();
config.Validate(); // Throws if invalid
var safe = config.ToSanitized(); // For logging
```

### 3. IAgentSubmissionService

**Location**: `Submission/IAgentSubmissionService.cs`

Interface for submitting tasks to OpenCode agents. Returns a `SubmissionResult` with three possible outcomes:
- **Success**: Task submitted, returns agent job ID
- **Duplicate**: Task already submitted (idempotency check)
- **Failure**: Submission failed with error details

**Example**:
```csharp
var result = submissionService.SubmitTask(task);
switch (result.ResultType) {
    case SubmissionResultType.Success:
        Console.WriteLine($"Job ID: {result.AgentJobId}");
        break;
    case SubmissionResultType.Duplicate:
        Console.WriteLine("Already submitted");
        break;
    case SubmissionResultType.Failure:
        Console.Error.WriteLine($"Error: {result.Message}");
        break;
}
```

### 4. OpenCodeAgentSubmissionService

**Location**: `Submission/OpenCodeAgentSubmissionService.cs`

Production implementation that submits tasks to OpenCode AI agents.

**Key Features**:
- Deduplication using in-memory hash set
- Builds formatted prompts from task data
- Integrates with OpenCode CLI/API
- Handles errors gracefully
- Logs all operations

**Configuration**:
- `openCodeExecutable`: Path to OpenCode CLI (default: "opencode")
- `agentName`: Which agent to invoke (default: "dotnet-feature-coder")
- `deduplicationWindowMinutes`: How long to remember submissions (default: 60)

**Example**:
```csharp
var service = new OpenCodeAgentSubmissionService(
    openCodeExecutable: "opencode",
    agentName: "dotnet-feature-coder",
    deduplicationWindowMinutes: 60
);
```

### 5. AgentIssueEventDispatcher

**Location**: `Dispatcher/IIssueEventDispatcher.cs`

Production dispatcher that orchestrates the entire flow:
1. Validates integration is enabled
2. Converts GitLabIssueEvent to AgentTask
3. Submits to OpenCodeAgentSubmissionService
4. Handles results and logs appropriately

**Example**:
```csharp
var dispatcher = new AgentIssueEventDispatcher(submissionService, config);
dispatcher.DispatchIssueEvent(issueEvent);
```

## Configuration

All configuration is stored in `Config/App.config`:

### GitLab Integration Settings

```xml
<!-- REQUIRED: GitLab instance base URL (must use HTTPS) -->
<add key="GitLab:BaseUrl" value="https://gitlab.example.com" />

<!-- REQUIRED: Webhook secret token -->
<add key="GitLab:WebhookSecret" value="your-secret-token-here" />

<!-- REQUIRED: Target repository for agent work -->
<add key="GitLab:TargetRepoUrl" value="https://github.com/yourorg/yourrepo" />

<!-- Optional: Target branch/ref (empty = default branch) -->
<add key="GitLab:TargetRepoRef" value="" />

<!-- Optional: Display name -->
<add key="GitLab:DisplayName" value="GitLab" />

<!-- Enable/disable integration (true/false) -->
<add key="GitLab:Enabled" value="true" />
```

### OpenCode Agent Settings

```xml
<!-- Path to OpenCode CLI executable -->
<add key="OpenCode:Executable" value="opencode" />

<!-- Agent name to invoke -->
<add key="OpenCode:AgentName" value="dotnet-feature-coder" />

<!-- Deduplication window in minutes -->
<add key="OpenCode:DeduplicationWindowMinutes" value="60" />
```

### Webhook Server Settings

```xml
<add key="Webhook:Host" value="localhost" />
<add key="Webhook:Port" value="8080" />
```

## Usage

### Running the Service

1. **Configure the service**:
   - Edit `Config/App.config` with your settings
   - Set `GitLab:Enabled` to `true` to enable agent submission
   - Ensure all REQUIRED fields are filled

2. **Start the service**:
   ```bash
   dotnet run
   # or
   ./GitLabWebhookReceiver.exe
   ```

3. **Service startup**:
   - Validates configuration
   - Creates appropriate dispatcher (Agent or Stub)
   - Starts HTTP server on configured host:port
   - Listens for GitLab webhook events

### Webhook Endpoint

**POST** `http://localhost:8080/webhooks/gitlab/issues`

**Headers**:
- `X-Gitlab-Token`: Your webhook secret
- `X-Gitlab-Event`: "Issue Hook"
- `Content-Type`: "application/json"

**Body**: GitLab issue webhook JSON payload

### Example Flow

1. GitLab sends issue webhook (e.g., issue opened)
2. Webhook receiver validates token and parses JSON
3. Dispatcher converts to AgentTask
4. Submission service checks for duplicates
5. If not duplicate, submits to OpenCode agent
6. Agent receives formatted prompt with:
   - Issue title and description
   - Source project information
   - Target repository to work in
   - Labels, assignees, and metadata
7. Agent performs work in target repository
8. Response sent back to GitLab (200 OK)

## Integration with OpenCode SDK

The service integrates with the OpenCode SDK (https://opencode.ai/docs/sdk/) by:

1. **CLI Invocation** (placeholder in current implementation):
   ```bash
   opencode --agent dotnet-feature-coder --prompt "..."
   ```

2. **HTTP API** (future enhancement):
   ```
   POST /api/agents/{agentName}/submit
   Body: { "prompt": "...", "context": {...} }
   ```

3. **Prompt Format**:
   The service generates structured prompts from GitLab issues:
   ```
   # GitLab Issue Task
   **Issue**: Fix authentication bug
   **Issue URL**: https://gitlab.example.com/project/issues/42
   **Source Project**: mygroup/myproject (ID: 123)
   ...
   ## Description
   (Issue description)

   ## Target Repository
   **Repository URL**: https://github.com/org/backend
   **Branch/Ref**: main

   ## Instructions
   Please implement the necessary changes...
   ```

## Deduplication

The service prevents duplicate submissions using a deduplication key:

```
{source_project_id}:{issue_iid}:{action}:{timestamp_ticks}
```

For example: `123:42:open:638234567890123456`

**Behavior**:
- First submission: Submitted to agent, returns Success
- Duplicate within window: Skipped, returns Duplicate
- After window expires: Treated as new submission

**Window**: Configurable via `OpenCode:DeduplicationWindowMinutes` (default: 60 minutes)

## Error Handling

The service handles errors gracefully at multiple levels:

1. **Configuration errors**: Falls back to stub dispatcher
2. **Validation errors**: Returns Failure result with details
3. **Submission errors**: Logs error, returns Failure result
4. **Dispatcher errors**: Catches exceptions, logs, returns 200 OK to GitLab

**Design principle**: Never fail the webhook response. GitLab should always receive a valid response to prevent retries.

## Testing

To test the implementation:

1. **Stub mode** (GitLab:Enabled = false):
   - Service starts with stub dispatcher
   - Logs events to console without submitting to agents
   - Good for testing webhook integration

2. **Agent mode** (GitLab:Enabled = true):
   - Service submits to OpenCode agents
   - Check logs for submission results
   - Verify agent job IDs are returned

3. **Duplicate detection**:
   - Send the same webhook twice
   - First should succeed, second should show duplicate

4. **Configuration validation**:
   - Try invalid URLs (HTTP instead of HTTPS)
   - Try missing required fields
   - Verify appropriate error messages

## Current Implementation Status

### Completed ✓
- ✓ Internal task schema (AgentTask)
- ✓ Integration configuration model (GitLabIntegrationConfig)
- ✓ Submission service interface (IAgentSubmissionService)
- ✓ OpenCode submission service (OpenCodeAgentSubmissionService)
- ✓ Production dispatcher (AgentIssueEventDispatcher)
- ✓ Configuration management (WebhookConfig)
- ✓ Program wiring and initialization
- ✓ Deduplication logic
- ✓ Error handling and logging
- ✓ Configuration file with all settings

### Placeholder Implementations
- OpenCode CLI invocation (InvokeOpenCodeCli method)
  - Current: Saves prompts to temp files for demonstration
  - Production: Would invoke actual OpenCode CLI or API

### Future Enhancements
- Real OpenCode CLI integration
- HTTP API integration option
- Persistent deduplication storage (database/Redis)
- Metrics and monitoring
- Async/await support
- Structured logging (Serilog)
- Unit tests for all components
- Integration tests for end-to-end flow

## Files Created/Modified

### New Files
- `Models/AgentTask.cs` - Internal task schema
- `Models/GitLabIntegrationConfig.cs` - Integration configuration
- `Submission/IAgentSubmissionService.cs` - Service interface
- `Submission/OpenCodeAgentSubmissionService.cs` - Service implementation
- `Submission/AGENT_SUBMISSION_SERVICE.md` - This documentation

### Modified Files
- `Config/WebhookConfig.cs` - Added integration config loading
- `Config/App.config` - Added all new configuration settings
- `Dispatcher/IIssueEventDispatcher.cs` - Added AgentIssueEventDispatcher
- `WebhookReceiver/Program.cs` - Wired up new services

## References

- OpenCode SDK Documentation: https://opencode.ai/docs/sdk/
- GitLab Webhook Documentation: https://docs.gitlab.com/ee/user/project/integrations/webhooks.html
- Repository: r1proto/agents
- Related Issues: #3 (Dispatcher), #4 (Task Schema), #5 (Integration Config)

## License

This code follows the same license as the parent repository.
