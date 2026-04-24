# Add GitLab issue webhook receiver

## Summary

Implement a lightweight, stateless HTTP endpoint that receives GitLab issue webhook POST
requests, validates the webhook secret/token, parses the JSON payload, and forwards valid
issue events to the dispatcher layer.

## Implementation Prompt

Before coding, inspect the repository for existing HTTP routing/controller patterns and follow
them consistently.

Implement a webhook receiver for GitLab issue events that:

1. Accepts `POST` requests on a dedicated GitLab webhook path (e.g. `/webhooks/gitlab/issues`).
2. Reads the `X-Gitlab-Token` header and validates it against the configured webhook secret.
   Return `401 Unauthorized` for invalid or missing tokens.
3. Reads and parses the JSON request body into the GitLab issue event model (see issue #2).
   Return `400 Bad Request` for malformed or unparseable payloads.
4. Inspects the `X-Gitlab-Event` header (or equivalent event type field):
   - Forward `issue` events with action `open` or `update` to the dispatcher layer (issue #3).
   - Return `200 OK` for unsupported/ignored event types and silently ignore them without
     passing them downstream.
5. Returns `200 OK` on successful forwarding to the dispatcher.
6. Keeps all request-handling logic in the receiver; no business logic belongs here.

The receiver must be stateless — it should not persist any state itself.

Note: The receiver must not call the agent submission path directly; it MUST forward parsed events to the dispatcher which is responsible for mapping, deduplication, and invoking the repository's canonical agent submission path. This keeps GitLab-specific logic out of the agent layer and preserves a single submission path.

## Acceptance Criteria

- [ ] A POST to the webhook endpoint with a valid token and `issue` event payload returns `200 OK`
      and triggers the dispatcher.
- [ ] A POST with an invalid or missing `X-Gitlab-Token` returns `401 Unauthorized` without
      reaching the dispatcher.
- [ ] A POST with a malformed JSON body returns `400 Bad Request`.
- [ ] A POST with an unsupported event type (e.g. `Push Hook`) returns `200 OK` and is silently
      ignored (no dispatcher call).
- [ ] Tests exist for: valid token, invalid or missing token, malformed JSON, unsupported event
      type.
- [ ] The receiver follows the existing HTTP routing/controller patterns in the repository.
- [ ] The receiver is stateless; it holds no mutable state between requests.
