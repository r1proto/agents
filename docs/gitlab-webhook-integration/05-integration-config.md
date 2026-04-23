# Add IT-managed GitLab integration configuration

## Summary

Implement a centralized, admin-managed GitLab integration configuration. The configuration
is not user-managed; only IT/admins can set or change it.

## Implementation Prompt

Before implementing, inspect the repository's existing config/storage patterns and match them.

Implement a GitLab integration configuration that:

1. Stores the following fields:
   - `base_url` — the GitLab instance base URL (e.g. `https://gitlab.company.internal`)
   - `webhook_secret` — the shared secret used to validate incoming webhook requests
   - `enabled` — boolean flag to enable or disable the integration without removing config
   - `display_name` — optional human-readable label for the GitLab instance

2. Supports exactly one configured GitLab instance in the initial version.

3. Validates on load/save:
   - `base_url` must be a valid `https://` URL; reject `http://` and non-URL values.
   - Trailing slashes in `base_url` are normalized away.
   - `webhook_secret` must be non-empty.

4. Stores `webhook_secret` securely — use the repository's existing secret/credential storage
   mechanism if one exists; otherwise store it encrypted or as an environment-variable reference
   rather than plaintext in a config file.

5. Exposes a read-only view that omits the raw secret value in any serialised output (logs,
   API responses).

The configuration must be admin-managed only. There must be no user-facing API or UI surface
to create or modify it.

## Acceptance Criteria

- [ ] The configuration model exists with all fields listed above.
- [ ] `base_url` validation rejects `http://`, empty strings, and non-URL values.
- [ ] Trailing slashes in `base_url` are normalized on load.
- [ ] `webhook_secret` is required and stored securely (not as plaintext in a config file).
- [ ] The raw secret value does not appear in serialised config output or logs.
- [ ] `enabled` defaults to `false` (opt-in).
- [ ] Only one GitLab instance is supported in this version.
- [ ] Tests cover: valid config, invalid `base_url` variants, missing secret, URL normalization.
- [ ] The implementation matches existing config/storage patterns in the repository.
