# Wire GitLab webhook events through dispatcher to OpenCode AI Agent

## Summary

Wire together the GitLab webhook receiver, event model, dispatcher, and agent submission path
so that a real GitLab issue webhook results in a task being submitted to the OpenCode AI Agent.

## Implementation Prompt

Before implementing, inspect the repository for the current HTTP entrypoint and agent execution
path. Integrate with them rather than creating duplicates.

The full flow to wire up is:

```
GitLab issue webhook POST
  └─► Webhook Receiver (issue #1)
        └─► GitLabIssueEvent parser/validator (issue #2)
              └─► Dispatcher Service (issue #3)
                    └─► Internal Task Schema (issue #4)
                          └─► Agent Submission Path (existing)
```

Wiring steps:

1. Register the webhook receiver route (issue #1) in the application's HTTP router/entrypoint.
2. Inject the GitLab integration configuration (issue #5) into the receiver so it can validate
   the webhook secret.
3. Wire the receiver's output into the event model parser (issue #2).
4. Wire the parser's output into the dispatcher (issue #3).
5. Wire the dispatcher's task payload output into the existing agent submission path.

Clarification: In the diagram the wavy connector labeled "???" represents the existing agent submission path. Do not introduce a new agent execution or submission mechanism. The dispatcher MUST call the repository's canonical agent submission path (in-process call or the established submission service). The three handwritten options shown in the diagram ("1 - manual mode", "2 - Agent Client Protocol", "3 - opencode SDK") are alternative approaches considered during design; they are NOT the accepted implementation. Prefer reusing the existing agent submission path. Also correct the diagram label to read "Agent submission path (existing)" and fix the spelling "Protocol" in diagram annotations where applicable.
6. Ensure errors at each step propagate and are returned with the appropriate HTTP status
   (400 for parse errors, 401 for auth failures, 500 for internal/dispatch failures).
7. Add structured logging at each stage: webhook received, event parsed, dispatched, submitted.

Add end-to-end or integration tests that:

Image: `diagram-corrected.svg` — updated to label the agent submission path explicitly and move design alternatives into an informational callout. See docs/gitlab-webhook-integration/diagram-corrected.svg


- Send a realistic GitLab issue webhook payload to the HTTP endpoint.
- Assert that the agent submission path is called with the expected task payload.
- Cover the error paths: invalid token, malformed payload, dispatcher failure.

## Acceptance Criteria

- [ ] The webhook receiver route is registered in the application's HTTP router.
- [ ] A real GitLab issue webhook POST (with valid token) results in an agent task submission.
- [ ] Invalid token returns `401`; malformed body returns `400`; dispatcher failure returns `500`.
- [ ] Structured log entries exist at: webhook received, event parsed, dispatched to agent.
- [ ] End-to-end or integration tests cover the happy path and the three error paths above.
- [ ] No new agent submission path is introduced; the existing one is reused.
- [ ] No GitLab-specific logic leaks into the agent submission layer.
