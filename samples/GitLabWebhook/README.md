# GitLab Issue Webhook Receiver

A lightweight, stateless ASP.NET Core 8 HTTP endpoint that receives GitLab issue webhook POST requests, validates the webhook secret/token, parses the JSON payload, and forwards valid issue events to the dispatcher layer.

## Structure

```
GitLabWebhook/
├── GitLabWebhook/                         # Web API project
│   ├── Controllers/
│   │   └── GitLabWebhookController.cs     # POST /webhooks/gitlab/issues
│   ├── Interfaces/
│   │   └── IIssueEventDispatcher.cs       # Dispatcher contract
│   ├── Models/
│   │   └── GitLabIssueEvent.cs            # GitLab issue event payload model
│   ├── Program.cs                         # Application entry point & DI wiring
│   └── appsettings.json                   # Configuration (GitLab:WebhookSecret)
└── GitLabWebhook.Tests/
    └── GitLabWebhookControllerTests.cs    # Integration tests
```

## Configuration

Set the `GitLab:WebhookSecret` configuration key to match the secret configured in your GitLab webhook settings. Use environment variables, user secrets, or `appsettings.json`:

```json
{
  "GitLab": {
    "WebhookSecret": "your-secret-token-here"
  }
}
```

## Endpoint

### `POST /webhooks/gitlab/issues`

| Header | Required | Description |
|---|---|---|
| `X-Gitlab-Token` | Yes | Must match the configured `GitLab:WebhookSecret`. Returns **401** on mismatch or absence. |
| `X-Gitlab-Event` | No | Only `Issue Hook` events are forwarded; all others return **200** and are silently ignored. |

| Condition | Response |
|---|---|
| Invalid or missing `X-Gitlab-Token` | **401 Unauthorized** |
| Malformed JSON body | **400 Bad Request** |
| Unsupported `X-Gitlab-Event` | **200 OK** (silently ignored) |
| `Issue Hook` with action `open` or `update` | **200 OK** + event forwarded to dispatcher |
| `Issue Hook` with any other action | **200 OK** (silently ignored) |

## Dispatcher

Implement `IIssueEventDispatcher` and register it in `Program.cs` to forward events to your processing layer. The default `NoOpIssueEventDispatcher` logs to the console for development purposes.

## Running Tests

```bash
dotnet test
```
