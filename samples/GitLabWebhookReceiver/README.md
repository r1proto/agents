# GitLab Issue Webhook Receiver

A lightweight, stateless HTTP endpoint that receives GitLab issue webhook POST requests, validates the webhook secret/token, parses the JSON payload, and forwards valid issue events to the dispatcher layer.

## Overview

This implementation provides a webhook receiver for GitLab issue events that:

- Accepts POST requests on `/webhooks/gitlab/issues`
- Validates the `X-Gitlab-Token` header against a configured webhook secret
- Parses the JSON request body into GitLab issue event models
- Forwards issue events with action `open` or `update` to the dispatcher layer
- Returns appropriate HTTP status codes for various scenarios
- Is completely stateless - no state is persisted between requests

## Project Structure

```
GitLabWebhookReceiver/
├── Config/               # Configuration management
│   ├── App.config       # Application configuration file
│   └── WebhookConfig.cs # Configuration helper class
├── Dispatcher/          # Event dispatcher interface and stub implementation
│   └── IIssueEventDispatcher.cs
├── Models/              # GitLab webhook event models
│   └── GitLabIssueEvent.cs
├── Tests/               # Unit and integration tests
│   ├── GitLabWebhookReceiverTests.cs
│   └── WebhookReceiverIntegrationTests.cs
├── WebhookReceiver/     # HTTP server and webhook handler
│   ├── GitLabWebhookReceiver.cs
│   ├── WebhookServer.cs
│   └── Program.cs
├── packages.txt         # Required NuGet packages
└── README.md           # This file
```

## Configuration

Edit `Config/App.config` to configure the webhook receiver:

```xml
<configuration>
  <appSettings>
    <!-- GitLab webhook secret token (X-Gitlab-Token) -->
    <add key="GitLab:WebhookSecret" value="your-secret-token-here" />

    <!-- Webhook receiver HTTP server settings -->
    <add key="Webhook:Host" value="localhost" />
    <add key="Webhook:Port" value="8080" />
  </appSettings>
</configuration>
```

**Important**: Set the `GitLab:WebhookSecret` to match the token configured in your GitLab webhook settings.

## Running the Receiver

1. Configure your webhook secret in `Config/App.config`
2. Compile the application
3. Run `Program.cs` from the `WebhookReceiver` directory
4. The server will listen on `http://localhost:8080/webhooks/gitlab/issues/`

## API Specification

### Endpoint

**POST** `/webhooks/gitlab/issues`

### Request Headers

- `X-Gitlab-Token` (required): The webhook secret token for authentication
- `X-Gitlab-Event` (required): The event type (must be `Issue Hook`)
- `Content-Type`: `application/json`

### Request Body

The request body should be a GitLab issue webhook event payload in JSON format. See [GitLab webhook documentation](https://docs.gitlab.com/ee/user/project/integrations/webhooks.html) for the complete payload structure.

### Response Codes

- **200 OK**: Event processed successfully or silently ignored (unsupported event type/action)
- **400 Bad Request**: Malformed or unparseable JSON payload
- **401 Unauthorized**: Invalid or missing `X-Gitlab-Token` header
- **405 Method Not Allowed**: Non-POST request
- **500 Internal Server Error**: Error during event processing

## Supported Events

The receiver processes the following GitLab issue events:

- **Issue opened** (`action: "open"`) - Forwarded to dispatcher
- **Issue updated** (`action: "update"`) - Forwarded to dispatcher
- **Other issue actions** - Silently ignored (returns 200 OK)
- **Non-issue events** - Silently ignored (returns 200 OK)

## Testing

The project includes comprehensive unit and integration tests:

### Unit Tests (`GitLabWebhookReceiverTests.cs`)

Tests for:
- JSON deserialization of GitLab issue events
- Dispatcher invocation
- Constructor parameter validation
- Malformed JSON handling

### Integration Tests (`WebhookReceiverIntegrationTests.cs`)

End-to-end HTTP tests for:
- Valid token with `open` and `update` actions → 200 OK + dispatcher called
- Invalid token → 401 Unauthorized
- Missing token → 401 Unauthorized
- Malformed JSON → 400 Bad Request
- Unsupported event type → 200 OK without dispatching
- Unsupported action → 200 OK without dispatching
- GET request → 405 Method Not Allowed

To run tests:
```bash
# Using dotnet CLI (if .csproj file is created)
dotnet test

# Using MSTest runner
vstest.console.exe Tests/GitLabWebhookReceiverTests.dll Tests/WebhookReceiverIntegrationTests.dll
```

## Dependencies

See `packages.txt` for the complete list of dependencies:

- **Newtonsoft.Json 13.0.3** - JSON serialization/deserialization
- **MSTest.TestFramework 3.1.1** - Unit testing framework
- **MSTest.TestAdapter 3.1.1** - Test adapter for MSTest
- **Microsoft.NET.Test.Sdk 17.8.0** - Test SDK
- **System.Configuration.ConfigurationManager 8.0.0** - Configuration management

## Dispatcher Integration

The webhook receiver forwards validated events to an `IIssueEventDispatcher` implementation. Currently, a `StubIssueEventDispatcher` is used that logs events to the console.

To integrate with the actual dispatcher layer (issue r1proto/agents#3):

1. Implement the `IIssueEventDispatcher` interface
2. Replace `StubIssueEventDispatcher` in `Program.cs` with your implementation
3. The dispatcher will receive validated `GitLabIssueEvent` objects

## Design Decisions

### Stateless Architecture

The webhook receiver maintains no state between requests. All request handling is done in memory, and the receiver simply acts as a validation and routing layer.

### Following Repository Patterns

This implementation follows the existing patterns found in the `RabbitMqOrderService` sample:

- **Message-based routing**: Similar to RabbitMQ routing key dispatch
- **Validation pattern**: Field-level validation with structured error responses
- **Configuration pattern**: Uses `ConfigurationManager.AppSettings` like `AppConfig.cs`
- **Error handling**: Try-catch with console logging, similar to `OrderServiceConsumer`
- **Testing pattern**: MSTest framework with validation and integration tests

### Security Considerations

- Token validation is performed before any payload processing
- Malformed JSON is rejected immediately
- No business logic is executed in the receiver
- All errors are logged but don't expose sensitive information

## Acceptance Criteria

✅ A POST to the webhook endpoint with a valid token and issue event payload returns 200 OK and triggers the dispatcher

✅ A POST with an invalid or missing `X-Gitlab-Token` returns 401 Unauthorized without reaching the dispatcher

✅ A POST with a malformed JSON body returns 400 Bad Request

✅ A POST with an unsupported event type (e.g. Push Hook) returns 200 OK and is silently ignored (no dispatcher call)

✅ Tests exist for: valid token, invalid or missing token, malformed JSON, unsupported event type

✅ The receiver follows the existing HTTP routing/controller patterns in the repository

✅ The receiver is stateless; it holds no mutable state between requests

## Future Enhancements

- Add webhook signature verification (HMAC-SHA256)
- Add request rate limiting
- Add metrics/monitoring support
- Add structured logging (e.g., using Serilog)
- Add support for async/await patterns
- Add support for other GitLab webhook event types

## Related Issues

- **Issue r1proto/agents#2**: GitLab issue event model (implemented in this PR)
- **Issue r1proto/agents#3**: Dispatcher layer (stub implementation provided, awaiting actual dispatcher)

## License

This code follows the same license as the parent repository.
