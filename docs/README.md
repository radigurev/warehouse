# Warehouse — Spec-Driven Development (SDD) Documentation

> Last updated: 2026-04-06

## Documentation Structure

This project uses a **two-tier documentation model** and conforms to **ISA-95 (IEC 62264)** for enterprise–operations integration. See `CLAUDE.md` section 1.1 for full ISA-95 compliance rules.

### Tier 1 — System Specs (`SDD-*`)

System specs describe the **current, implemented behavior** of the system. They are the source of truth for what the code does today. Each spec has a unique ID and lives in a categorized subfolder. All specs MUST reference applicable ISA-95 parts/sections in their Context block.

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

| ID | Category | Title | ISA-95 Reference | Status |
|---|---|---|---|---|
| SDD-AUTH-001 | Infrastructure | Authentication and Authorization | Part 2, Section 6 — Personnel Model | Implemented |
| SDD-UI-001 | Core | Auth Administration SPA | — | Implemented |
| SDD-UI-002 | Domain | Form Display Mode (Modal vs Page) | — | Draft |
| SDD-UI-003 | Domain | User Settings | — | Draft |
| SDD-CUST-001 | Domain | Customers and Accounts | — | Implemented |
| SDD-INV-001 | Core | Products and Catalog | Part 2, Section 7 — Material Model | Active |
| SDD-INV-002 | Core | Stock Management | Part 2 Section 7 + Part 3 — Inventory Operations | Active |
| SDD-INV-003 | Core | Warehouse Structure | Part 2 Section 5 + Part 3 — Inventory Operations | Active |
| SDD-INV-004 | Core | Stocktaking | Part 3 — Inventory Counting | Active |

## Change Spec Registry

| ID | Title | Affected System Specs | Status |
|---|---|---|---|
| CHG-ENH-001 | Auth SPA UI Redesign | SDD-UI-001 | Implemented |
| CHG-ENH-002 | Audit UX and Navigation Improvements | SDD-UI-001, SDD-AUTH-001 | Implemented |
| CHG-ENH-003 | Server-Side Pagination | SDD-AUTH-001, SDD-CUST-001, SDD-INV-001 | Implemented |
| CHG-REFAC-001 | Infrastructure Extraction and Service Split | SDD-AUTH-001 | Implemented |

## Status Legend

| Status | Meaning |
|---|---|
| `Draft` | Spec is being written, not yet reviewed |
| `Review` | Spec is ready for review |
| `Approved` | Spec is approved, implementation can begin |
| `Active` | Spec describes current implemented behavior (actively maintained) |
| `Implemented` | Spec describes current implemented behavior |
| `Deprecated` | Spec is no longer active |

## ID Allocation — Next Available

| Prefix | Next ID |
|---|---|
| `SDD-CUST` | 002 |
| `SDD-INV` | 005 |
| `SDD-UI` | 004 |
| `SDD-AUTH` | 002 |
| `SDD-FILT` | 001 |
| `SDD-OBS` | 001 |
| `CHG-FEAT` | 001 |
| `CHG-ENH` | 004 |
| `CHG-FIX` | 001 |
| `CHG-REFAC` | 002 |
| `CHG-DEBT` | 001 |
