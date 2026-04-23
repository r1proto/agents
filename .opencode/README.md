# PRODUCTION-GRADE OPENCODE AGENT PACK TEMPLATE

This directory is a production-grade template for an OpenCode agent pack.
Customize all placeholders, models, permissions, and validation commands before production use.

## Included files

### General-purpose agents
- `opencode.jsonc` — main agent configuration
- `prompts/build-specialist.txt` — build agent prompt (pre-configured for .NET 8)
- `agents/reviewer.md` — reviewer subagent (structured 10-point code review)

### Spec-to-code flow agents (GitLab issue → .NET 8 / RabbitMQ / EFCore)
- `agents/design-gate.md` — Gate 1: assesses whether the technical spec is detailed enough to begin coding
- `agents/coding-gate.md` — Gate 2: assesses whether the coding agent has enough context (spec + codebase) to work safely
- `agents/dotnet-feature-coder.md` — coding agent for .NET 8 / RabbitMQ / EFCore feature implementation
- `agents/reviewer.md` — Gate 3: reviews the completed implementation before it is declared done

## Spec-to-code flow overview

```
GitLab Issue
  (spec_url, code_repo_url, docs_url)
        │
        ▼
  [design-gate]  ← Gate 1: Design Sufficiency
  PASS / FAIL
        │ PASS
        ▼
  [coding-gate]  ← Gate 2: Coding Readiness
  PASS / FAIL
        │ PASS
        ▼
  [dotnet-feature-coder]
  Implement → Verify (build + tests + lint)
        │
        ▼
  [reviewer]  ← Gate 3: Code Review
  Approve / Concerns / Block
        │ Approve
        ▼
  Implementation Summary
```

The issue and the code repository may belong to different GitLab projects. The `code_repo_url` in the issue determines where code changes are made.

## Recommended usage
1. Replace `REPLACE_WITH_MODEL` placeholders in `opencode.jsonc`.
2. Configure allowed tools and permission prompts.
3. Update the verification commands in `prompts/build-specialist.txt` if your project uses a non-standard structure.
4. Add additional specialized agents as needed.
5. Test the pack in a non-production environment first.