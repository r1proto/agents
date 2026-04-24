# Document GitLab webhook setup for internal deployment

## Summary

Add documentation explaining how IT/admins should configure GitLab webhooks for this
integration in a firewall-bound, internal deployment model.

## Implementation Prompt

Create a documentation page (Markdown) targeting IT administrators. The document should cover:

### 1. Overview
- What this integration does: GitLab issue events → dispatcher → OpenCode AI Agent.
- Scope: internal deployment only; users do not configure GitLab credentials themselves.

### 2. Prerequisites
- The OpenCode AI Agent deployment must be reachable from the GitLab instance (network/firewall
  allowlist if needed).
- An admin account on the GitLab instance to configure webhooks.

### 3. Configuration steps
1. Generate a random webhook secret (at least 32 characters).
2. Set the secret in the OpenCode AI Agent configuration (`webhook_secret`).
3. Set the GitLab base URL in the configuration (`base_url`).
4. Enable the integration (`enabled: true`).
5. In GitLab, navigate to **Project → Settings → Webhooks** (or group/instance-level webhooks
   as appropriate).
6. Set the **URL** to: `https://<opencode-host>/webhooks/gitlab/issues`
7. Set the **Secret token** to the same value configured in step 1.
8. Under **Trigger**, enable **Issues events** only. Disable all other event types.
9. Save the webhook. Use **Test → Issues Events** to send a test payload.

### 4. Supported event types
- `Issues events` — `open` and `update` actions are forwarded to the dispatcher.
- All other GitLab event types are acknowledged (`200 OK`) and silently ignored.

### 5. Example GitLab issue webhook payload (abbreviated)
Include a short, illustrative example of the JSON payload GitLab sends for an issue event.
Do not duplicate the full GitLab API documentation.

### 6. Security notes
- Use `https://` for the webhook URL.
- The webhook secret is validated on every request; requests with a missing or incorrect token
  are rejected with `401 Unauthorized`.
- Users do not have access to the webhook secret or the integration configuration.

### 7. Troubleshooting
- `401 Unauthorized` — secret mismatch; verify the token matches on both sides.
- `400 Bad Request` — malformed payload; check the GitLab version and event type.
- No dispatch after a valid webhook — verify `enabled: true` in the integration config.

## Acceptance Criteria

- [ ] Documentation exists as a Markdown file in the `docs/` tree.
- [ ] All 7 sections above are covered.
- [ ] The document clearly states that users do not configure GitLab credentials.
- [ ] The document is specific to the internal, firewall-bound deployment model.
- [ ] An abbreviated example webhook payload is included.
- [ ] The document does not duplicate the full GitLab API/webhook documentation.
