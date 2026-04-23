# Define GitLab issue webhook event model

## Summary

Define a typed internal model for GitLab issue webhook events, along with parsing and
validation logic that converts raw GitLab webhook payloads into the internal model.

## Implementation Prompt

Define an internal event model that captures the minimum fields needed for dispatch from a
GitLab issue webhook payload. The model should include:

| Field | Source in GitLab payload |
|-------|--------------------------|
| Project ID | `project.id` |
| Project path | `project.path_with_namespace` |
| Issue IID | `object_attributes.iid` |
| Issue title | `object_attributes.title` |
| Description | `object_attributes.description` |
| Labels | `labels[].title` |
| Author | `user.username` |
| Assignees | `assignees[].username` |
| State | `object_attributes.state` |
| Web URL | `object_attributes.url` |
| Action / event type | `object_attributes.action` |
| Timestamp | `object_attributes.created_at` / `updated_at` |

Add parsing logic that:

1. Deserialises a raw GitLab webhook JSON payload into the internal event model.
2. Validates that all required fields are present and non-empty (project ID, project path,
   issue IID, title, web URL, action).
3. Returns a typed validation error when required fields are missing.
4. Handles optional fields (labels, assignees, description) gracefully when absent.

Reference the [GitLab Issues API webhook payload](https://docs.gitlab.com/ee/user/project/integrations/webhook_events.html#issue-events)
for the canonical payload shape.

## Acceptance Criteria

- [ ] A typed internal model exists with all fields listed above.
- [ ] Parsing converts a raw GitLab webhook JSON payload into the model without panics or
      unhandled exceptions.
- [ ] Required-field validation returns a clear error when any required field is absent.
- [ ] Optional fields (labels, assignees, description) default to empty/zero values when absent.
- [ ] Tests cover: full valid payload, missing required fields, empty optional fields, and the
      field mappings expected by the GitLab Issue Events webhook format.
