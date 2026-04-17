# Warehouse — Spec-Driven Development (SDD) Documentation

> Last updated: 2026-04-17

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
| SDD-UI-004 | Core | Purchasing SPA | — | Implemented |
| SDD-UI-005 | Core | Fulfillment SPA | — | Implemented |
| SDD-CUST-001 | Domain | Customers and Accounts | — | Implemented |
| SDD-NOM-001 | Domain | Nomenclature Reference Data | Cross-cutting (reference data) | Implemented |
| SDD-INV-001 | Core | Products and Catalog | Part 2, Section 7 — Material Model | Implemented |
| SDD-INV-002 | Core | Stock Management | Part 2 Section 7 + Part 3 — Inventory Operations | Implemented |
| SDD-INV-003 | Core | Warehouse Structure | Part 2 Section 5 + Part 3 — Inventory Operations | Implemented |
| SDD-INV-004 | Core | Stocktaking | Part 3 — Inventory Counting | Implemented |
| SDD-INV-005 | Core | Batch Creation on Goods Receipt | Part 2 Section 7 — Material Lot | Implemented |
| SDD-PURCH-001 | Core | Procurement Operations | Part 3 — Material Receipt | Implemented |
| SDD-FULF-001 | Core | Fulfillment Operations | Part 3 — Material Shipment | Implemented |
| SDD-INFRA-001 | Infrastructure | Shared Infrastructure & Middleware | Cross-cutting (all parts) | Implemented |
| SDD-INFRA-002 | Infrastructure | API Gateway | Cross-cutting | Implemented |
| SDD-INFRA-003 | Infrastructure | Sequence Generation | Cross-cutting | Implemented |
| SDD-EVTLOG-001 | Infrastructure | Centralized Event Logging | Cross-cutting (Part 3 — Operations Events) | Implemented |
| SDD-OBS-001 | Infrastructure | Observability | Cross-cutting (Part 3 — Operations Events) | Implemented |
| SDD-COMP-001 | Infrastructure | Multi-Company Support | Part 2, Section 5 — Equipment Model (Enterprise) | Draft |

## Change Spec Registry

| ID | Title | Affected System Specs | Status |
|---|---|---|---|
| CHG-ENH-001 | Auth SPA UI Redesign | SDD-UI-001 | Implemented |
| CHG-ENH-001-NOM | Nomenclature Integration | SDD-NOM-001, SDD-CUST-001, SDD-PURCH-001, SDD-FULF-001 | Implemented |
| CHG-ENH-002 | Audit UX and Navigation Improvements | SDD-UI-001, SDD-AUTH-001 | Implemented |
| CHG-ENH-003 | Server-Side Pagination | SDD-AUTH-001, SDD-CUST-001, SDD-INV-001 | Implemented |
| CHG-ENH-004 | Realtime Permission Validation | SDD-AUTH-001, SDD-UI-001 | Implemented |
| CHG-ENH-005 | Error Code Localization Contract | SDD-INFRA-001, SDD-AUTH-001, SDD-CUST-001, SDD-INV-001–004, SDD-UI-001 | Implemented |
| CHG-ENH-006 | Correlation ID Propagation Gaps | SDD-INFRA-001, SDD-OBS-001 | Implemented |
| CHG-REFAC-001 | Infrastructure Extraction and Service Split | SDD-AUTH-001 | Implemented |
| CHG-REFAC-002 | Move Gateway to Infrastructure | SDD-INFRA-002 | Implemented |
| CHG-REFAC-003 | State Pattern — Workflow State Machines | SDD-PURCH-001, SDD-FULF-001 | Implemented |
| CHG-REFAC-004 | Template Method — Service Base Classes | SDD-INFRA-001 | Implemented |
| CHG-REFAC-005 | Strategy Pattern — Movement, Sorting, Mapping | SDD-INV-002, SDD-EVTLOG-001 | Implemented |
| CHG-REFAC-006 | Chain of Responsibility — Validation Pipelines | SDD-INFRA-001 | Implemented |
| CHG-REFAC-007 | Decorator Pattern — Cache and Event Resilience | SDD-INFRA-001 | Implemented |
| CHG-REFAC-008 | Factory Pattern — Generic EventLog Consumer | SDD-EVTLOG-001 | Implemented |
| CHG-REFAC-009 | Facade Pattern — StockLevelManager | SDD-INV-002 | Implemented |
| CHG-REFAC-010 | Builder & Bridge — Entity Construction | SDD-PURCH-001, SDD-FULF-001 | Implemented |
| CHG-REFAC-011 | Frontend CRUD API Factory and List View Composable | SDD-UI-001 | Implemented |
| CHG-REFAC-012 | Frontend Navigation Strategy and Utilities | SDD-UI-001 | Implemented |
| CHG-REFAC-013 | SOLID Principle Violation Fixes | Cross-cutting | Implemented |

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
| `SDD-AUTH` | 002 |
| `SDD-CUST` | 002 |
| `SDD-NOM` | 002 |
| `SDD-INV` | 006 |
| `SDD-PURCH` | 002 |
| `SDD-FULF` | 002 |
| `SDD-UI` | 006 |
| `SDD-COMP` | 002 |
| `SDD-INFRA` | 004 |
| `SDD-EVTLOG` | 002 |
| `SDD-OBS` | 002 |
| `SDD-FILT` | 001 |
| `CHG-FEAT` | 001 |
| `CHG-ENH` | 007 |
| `CHG-FIX` | 001 |
| `CHG-REFAC` | 014 |
| `CHG-DEBT` | 001 |
