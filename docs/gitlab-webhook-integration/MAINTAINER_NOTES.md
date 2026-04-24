Summary
-------
This change clarifies an ambiguity in the end-to-end wiring diagram for the GitLab webhook integration.

Problem
-------
The diagram contained a wavy connector labeled "???" between the dispatcher/receiver path and the OpenCode Agent and three handwritten alternative approaches. That created ambiguity about whether to reuse the repository's existing agent submission path or to implement a new integration.

What I changed
--------------
- docs/gitlab-webhook-integration/07-end-to-end-wiring.md: added a clarification that the wavy connector represents the existing agent submission path and that the dispatcher MUST reuse the repository's canonical agent submission path.
- docs/gitlab-webhook-integration/01-webhook-receiver.md: added a note that the receiver must not call the agent submission path directly and must forward parsed events to the dispatcher.
- docs/gitlab-webhook-integration/diagram-corrected.svg: new annotated diagram that explicitly labels the agent submission path as "existing — reuse" and moves the three design alternatives into an informational callout (they are not the accepted implementation).

Why this matters
-----------------
- Prevents implementers from introducing duplicate or provider-specific agent submission mechanisms.
- Keeps GitLab-specific logic confined to the event-model and dispatcher layers, preserving a provider-agnostic agent submission interface.

Verification steps
------------------
1. Integration tests:
   - POST a realistic GitLab `issue` event to `/webhooks/gitlab/issues` with a valid `X-Gitlab-Token` and assert that the agent submission path is invoked with the expected internal task payload.
   - Negative tests: invalid token → 401; malformed JSON → 400; unsupported event type → 200 OK and no dispatcher call.
   - Dedup test: send same event twice and confirm second is marked duplicate and agent not called again.

2. Manual smoke test (curl):
   - curl -v -X POST 'https://<host>/webhooks/gitlab/issues' \
       -H 'Content-Type: application/json' \
       -H 'X-Gitlab-Token: <VALID_SECRET>' \
       --data-binary @sample-issue-event.json
   - Expect structured logs for: webhook received, event parsed, dispatched, submitted.

Notes for reviewers
-------------------
- This is a documentation-only change; no runtime code was modified.
- If reviewers want to adopt one of the design alternatives (manual mode, client protocol, SDK), do so in a separate RFC/PR that includes implementation details and migration steps.

Contact
-------
For questions or follow-ups: rqiang (repo maintainer) or the OpenCode integration owners.
