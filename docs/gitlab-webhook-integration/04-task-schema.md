# Add internal task schema for GitLab issue dispatch

## Summary

Define the internal task schema used to represent a GitLab issue dispatched to the OpenCode
AI Agent. The schema must be stable and provider-agnostic at the agent layer.

## Implementation Prompt

Define an internal task schema (struct/type/interface) that the dispatcher (issue #3) produces
and the agent submission path consumes.

> **Key distinction:** The *source project* is the GitLab project where the issue is filed.
> The *target project* is the codebase repository the OpenCode AI Agent will actually work in
> to resolve the issue. These are often different repositories and must be modelled separately.

The schema has two logical groups of fields:

### Source — GitLab issue metadata

| Field | Description |
|-------|-------------|
| `source` | Fixed value `"gitlab"` |
| `instance_url` | Base URL of the GitLab instance |
| `source_project_id` | Numeric ID of the GitLab project where the issue is filed |
| `source_project_path` | Full path of the GitLab project (e.g. `group/subgroup/tracker`) |
| `issue_iid` | Project-scoped issue identifier |
| `title` | Issue title |
| `description` | Issue body/description |
| `labels` | List of label names |
| `author` | Issue author username |
| `assignees` | List of assignee usernames |
| `state` | Issue state (`opened`, `closed`, etc.) |
| `web_url` | Human-visible URL of the GitLab issue |
| `event_action` | Webhook action that triggered dispatch (`open`, `update`, etc.) |
| `event_timestamp` | ISO-8601 timestamp of the triggering event |

### Target — codebase the agent should work on

| Field | Description |
|-------|-------------|
| `target_repo_url` | URL of the repository the agent should clone/check out and modify |
| `target_repo_ref` | Branch or ref the agent should start from (optional; defaults to default branch) |

`target_repo_url` is a required field. It is provided by the IT-managed integration
configuration (issue #5), not inferred from the GitLab issue payload — because the issue
tracker project and the code repository are separate entities (e.g., `group/tracker` vs
`group/backend`).

Design constraints:

- The schema must be compact and stable; the agent layer must not need to know about GitLab
  payload details.
- Optional fields (description, labels, assignees, target_repo_ref) should use empty/zero
  values, not nullable types, to keep the schema simple.
- Add a mapping function/constructor that takes a `GitLabIssueEvent` (issue #2) and the
  integration configuration (issue #5) and returns a populated task schema instance.

## Acceptance Criteria

- [ ] The internal task schema exists with all fields listed above, organised into source and
      target sections.
- [ ] `source_project_id`, `source_project_path`, and `target_repo_url` are separate fields —
      the schema makes it impossible to conflate where the issue came from with where the agent
      should work.
- [ ] `target_repo_url` is required and sourced from the integration configuration, not the
      GitLab webhook payload.
- [ ] A mapping function/constructor accepts a `GitLabIssueEvent` plus integration config and
      returns a populated task schema instance.
- [ ] Optional fields default to empty/zero values when absent in the source event.
- [ ] The schema contains no GitLab API-specific types or imports.
- [ ] Tests cover: full event with target config, minimal event (only required fields), empty
      labels/assignees, and verification that `target_repo_url` is always populated from config.
