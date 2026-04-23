# GitLab Webhook Integration — Planned Issues

This directory tracks the planned implementation work for the IT-managed, self-hosted GitLab
integration where GitLab issue webhooks trigger a dispatcher that forwards normalized tasks to
the OpenCode AI Agent.

## Context

The system is internal-only; users do not manage GitLab credentials. IT/admins configure and
manage the GitLab integration. The initial scope is limited to GitLab issue events.

## Issues

| # | Title | File |
|---|-------|------|
| 1 | Add GitLab issue webhook receiver | [01-webhook-receiver.md](01-webhook-receiver.md) |
| 2 | Define GitLab issue webhook event model | [02-event-model.md](02-event-model.md) |
| 3 | Add dispatcher service for GitLab issue events | [03-dispatcher-service.md](03-dispatcher-service.md) |
| 4 | Add internal task schema for GitLab issue dispatch | [04-task-schema.md](04-task-schema.md) |
| 5 | Add IT-managed GitLab integration configuration | [05-integration-config.md](05-integration-config.md) |
| 6 | Document GitLab webhook setup for internal deployment | [06-webhook-setup-docs.md](06-webhook-setup-docs.md) |
| 7 | Wire GitLab webhook events through dispatcher to OpenCode AI Agent | [07-end-to-end-wiring.md](07-end-to-end-wiring.md) |

## Flow

```
GitLab (issue event)
  └─► Webhook Receiver (#1)
        └─► Event Model Parser (#2)
              └─► Dispatcher Service (#3)
                    └─► Internal Task Schema (#4)
                          └─► OpenCode AI Agent
```

Configuration (#5) and Documentation (#6) support the full flow.
End-to-end wiring (#7) integrates all components.
