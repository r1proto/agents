# Add dispatcher service for GitLab issue events

## Summary

Implement the dispatcher service that receives a normalized GitLab issue event, performs
deduplication/idempotency checks, maps the event into an internal task payload, and invokes
the agent submission workflow.

## Implementation Prompt

Before implementing, inspect the repository for the current agent execution/submission path
and integrate with it rather than creating a parallel mechanism.

Implement a dispatcher service that:

1. Accepts a parsed `GitLabIssueEvent` (from issue #2) as input.
2. Constructs an internal task payload (from issue #4) from the event.
3. Performs an idempotency check before submitting:
   - Derive a deterministic deduplication key from `(project_id, issue_iid, action, timestamp)`.
   - If the same event has already been submitted (within a reasonable window), skip submission
     and return a `duplicate` result without error.
4. Calls the existing agent submission path with the task payload.
5. Returns a dispatch result indicating success, duplicate, or failure.
6. Handles downstream agent submission failures gracefully — log the error, return a failure
   result, and do not swallow the error silently.

Keep GitLab-specific logic entirely within the dispatcher and event model layers; the agent
submission path must remain provider-agnostic.

## Acceptance Criteria

- [ ] The dispatcher converts a `GitLabIssueEvent` into an internal task payload and submits it
      to the agent.
- [ ] Submitting the same event twice (same deduplication key) results in one submission; the
      second call returns a `duplicate` result without calling the agent again.
- [ ] A failure in the downstream agent submission is returned as a failure result, not silently
      ignored.
- [ ] The agent submission path contains no GitLab-specific code.
- [ ] Tests cover: successful dispatch, duplicate detection, downstream agent failure.
- [ ] The dispatcher reuses the repository's current agent submission path.
