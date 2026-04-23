# Add internal task schema for GitLab issue dispatch

## Summary

Define the internal task schema used to represent a GitLab issue dispatched to the OpenCode
AI Agent. The schema must be stable and provider-agnostic at the agent layer.

## Implementation Prompt

Define an internal task schema (struct/type/interface) that the dispatcher (issue #3) produces
and the agent submission path consumes. The schema should include:

| Field | Description |
|-------|-------------|
| `source` | Fixed value `"gitlab"` |
| `instance_url` | Base URL of the GitLab instance |
| `project_id` | Numeric GitLab project ID |
| `project_path` | Full project path (e.g. `group/subgroup/repo`) |
| `issue_iid` | Project-scoped issue identifier |
| `title` | Issue title |
| `description` | Issue body/description |
| `labels` | List of label names |
| `author` | Issue author username |
| `assignees` | List of assignee usernames |
| `state` | Issue state (`opened`, `closed`, etc.) |
| `web_url` | Human-visible URL of the issue |
| `event_action` | Webhook action that triggered dispatch (`open`, `update`, etc.) |
| `event_timestamp` | ISO-8601 timestamp of the triggering event |

Design constraints:

- The schema must be compact and stable; the agent layer must not need to know about GitLab
  payload details.
- Optional fields (description, labels, assignees) should use empty/zero values, not nullable
  types, to keep the schema simple.
- Add a mapping function/constructor that takes a `GitLabIssueEvent` (issue #2) and returns a
  populated task schema instance.

## Acceptance Criteria

- [ ] The internal task schema exists with all fields listed above.
- [ ] A mapping function/constructor converts a `GitLabIssueEvent` into the task schema.
- [ ] Optional fields default to empty/zero values when absent in the source event.
- [ ] The schema contains no GitLab API-specific types or imports.
- [ ] Tests cover mapping from a full event, a minimal event (only required fields), and edge
      cases like empty labels and assignees lists.
