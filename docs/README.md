# Warehouse — Spec-Driven Development (SDD) Documentation

> Last updated: 2026-04-02

## Documentation Structure

This project uses a **two-tier documentation model**:

### Tier 1 — System Specs (`SDD-*`)

System specs describe the **current, implemented behavior** of the system. They are the source of truth for what the code does today. Each spec has a unique ID and lives in a categorized subfolder.

### Tier 2 — Change Specs (`CHG-*`)

Change specs describe **proposed changes** to the system. They reference the system specs they affect and include migration plans, rollback strategies, and acceptance criteria. Change specs live in `docs/changes/`.

## Workflow

```
spec-writer → implementator → tester → spec-validator
```

1. **Spec Writer** — Write or update the SDD specification before any code is written.
2. **Implementator** — Write production-ready code that fulfills the specification.
3. **Tester** — Write tests that verify the implementation matches the specification.
4. **Spec Validator** — Validate that the spec, code, and tests are all in sync.

## Conventions

### Spec ID Format

- System specs: `SDD-<DOMAIN>-<NNN>` (e.g., `SDD-INV-001`, `SDD-AUTH-001`)
- Change specs: `CHG-<TYPE>-<NNN>` (e.g., `CHG-FEAT-001`, `CHG-FIX-002`)

### Change Spec Prefixes

| Prefix | Use For |
|---|---|
| `CHG-FEAT-NNN` | New features or capabilities |
| `CHG-ENH-NNN` | Enhancements to existing behavior |
| `CHG-FIX-NNN` | Bug fixes |
| `CHG-REFAC-NNN` | Refactoring (no behavior change) |
| `CHG-DEBT-NNN` | Technical debt reduction |

### Test Naming

Test classes reference spec IDs: `{TestClassName}_SDD_{DOMAIN}_{NNN}_{Description}`

### XML Doc References

```csharp
/// <para>
/// Specification reference:
/// SDD-XXX-NNN – Title (see docs/{category}/SDD-XXX-NNN-description.md).
/// </para>
```

## Directory Structure

| Path | Purpose |
|---|---|
| `docs/core/` | Core business pipeline — primary workflows and entities |
| `docs/domain/` | Domain-specific capabilities — secondary features, metadata |
| `docs/integration/` | External system integrations — ERP, external APIs |
| `docs/infrastructure/` | Cross-cutting concerns — auth, filtering, observability |
| `docs/changes/` | Change specs (proposed modifications to the system) |

## System Spec Registry

| ID | Category | Title | Status |
|---|---|---|---|
| — | — | No specs yet — project is in scaffolding phase | — |

## Change Spec Registry

| ID | Title | Affected System Specs | Status |
|---|---|---|---|
| — | — | — | — |

## Status Legend

| Status | Meaning |
|---|---|
| `Draft` | Spec is being written, not yet reviewed |
| `Review` | Spec is ready for review |
| `Approved` | Spec is approved, implementation can begin |
| `Implemented` | Spec describes current implemented behavior |
| `Deprecated` | Spec is no longer active |

## ID Allocation — Next Available

| Prefix | Next ID |
|---|---|
| `SDD-INV` | 001 |
| `SDD-WH` | 001 |
| `SDD-SHIP` | 001 |
| `SDD-AUTH` | 001 |
| `SDD-FILT` | 001 |
| `SDD-OBS` | 001 |
| `CHG-FEAT` | 001 |
| `CHG-ENH` | 001 |
| `CHG-FIX` | 001 |
| `CHG-REFAC` | 001 |
| `CHG-DEBT` | 001 |
