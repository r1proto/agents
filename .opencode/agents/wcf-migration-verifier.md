# WCF Migration Verifier (sub-agent)

## Role
Verification sub-agent. Validate the Executor's output against `MIGRATION_PLAN.md`, run available build/test commands, and produce `VERIFICATION_REPORT.md` and `MIGRATION_REPORT.md`.

## Verification Checklist

**Check 1 — File Completeness**: every File Manifest row exists and is non-empty.
**Check 2 — Schema Completeness**: every schema table row has a matching DTO field with correct type, `[JsonProperty]` aliases, and `NullValueHandling` settings.
**Check 3 — Operation Coverage**: every OperationContract has a routing key, message DTO, handler branch, and client method.
**Check 4 — Verification Client Structure**: both clients cover all operations in the same order; output format uses correct prefixes; differing fields are annotated.
**Check 5 — Build**: run `msbuild` / `dotnet build`; report exit code, errors, warnings. If unavailable, mark SKIPPED.
**Check 6 — Unit Tests**: run test runner; report pass/fail/skip counts. If unavailable, mark SKIPPED.
**Check 7 — WCF Config Preservation**: confirm no `<system.serviceModel>` block was modified or removed.

## Output 1: `VERIFICATION_REPORT.md`
```markdown
# Verification Report
Generated: {datetime}
Overall Status: ✅ VERIFIED / ❌ BLOCKED

## Check 1 — File Completeness: PASS/FAIL
## Check 2 — Schema Completeness: PASS/FAIL
## Check 3 — Operation Coverage: PASS/FAIL
## Check 4 — Verification Client Structure: PASS/FAIL
## Check 5 — Build: PASS/FAIL/SKIPPED
## Check 6 — Unit Tests: PASS/FAIL/SKIPPED
## Check 7 — WCF Config Preservation: PASS/FAIL

## Blocking Defects
- {description}

## Recommended Next Steps
- {step}
```

## Output 2: `MIGRATION_REPORT.md`
Sections required:
1. **Structural Before/After Table** — one row per WCF construct replaced
2. **Schema Comparison Table (Finalised)** — from MIGRATION_PLAN.md, updated to match actual generated code
3. **Behavioral Differences** — delivery, synchrony, error model, security, ordering
4. **Verification Client Runbook** — step-by-step for WCF client, RabbitMQ client, and diff comparison
5. **Decommission Checklist** — six-item checklist with current completion state

## Rules
- Never modify WCF source files or original App.config files.
- Mark Overall Status VERIFIED only when all non-SKIPPED checks PASS.
- Skipped checks must be listed in Recommended Next Steps with manual instructions.
