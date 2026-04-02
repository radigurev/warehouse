# Warehouse — Project Instructions

> Last updated: 2026-04-02

---

## 0. Active Personas & Workflow

### Execution Pipeline

```
spec-writer → implementator → tester → spec-validator
```

| Phase | Persona Category | Base Persona | Purpose |
|---|---|---|---|
| **1. Spec Writer** | `~/.claude/personas/spec-writer/` | `doc-governance.md` | Write or update the SDD specification before any code is written |
| **2. Implementator** | `~/.claude/personas/implementator/` | `csharp-persona.md` | Write production-ready code that fulfills the specification |
| **3. Tester** | `~/.claude/personas/tester/` | `csharp-persona.md` | Write tests that verify the implementation matches the specification |
| **4. Spec Validator** | `~/.claude/personas/validator/` | `doc-governance.md` | Validate that the spec, code, and tests are all in sync |

### Implementator Personas

| Concern | Persona File |
|---|---|
| Base code standards | `~/.claude/personas/csharp-persona.md` |
| .NET 8 REST Microservices | `~/.claude/personas/implementator/persona-dotnet8-microservices.md` |
| Database (EF Core, migrations) | `~/.claude/personas/implementator/persona-database.md` |
| MongoDB (if added later) | `~/.claude/personas/implementator/persona-mongodb.md` |

### Tester Personas

| Concern | Persona File |
|---|---|
| Base (always-on) | `~/.claude/personas/tester/tester-base.md` |
| BLL unit tests | `~/.claude/personas/tester/tester-bll.md` |
| EF Core / ORM tests | `~/.claude/personas/tester/tester-ef-orm.md` |
| API integration tests | `~/.claude/personas/tester/tester-integration.md` |

### Precedence

```
CLAUDE.md > persona-database > persona-dotnet8-microservices > csharp-persona > doc-governance
```

---

## 0.1 Project Policies (override persona defaults)

- **Target framework:** .NET 8.0
- **Error responses:** ProblemDetails (RFC 7807)
- **Polly:** Required on all outbound HTTP clients (to be added when services are implemented)
- **Mapping:** AutoMapper only
- **Logging:** NLog with structured message templates
- **ORM:** EF Core (to be added)
- **Validation:** FluentValidation (to be added)
- **PK strategy:** INT IDENTITY (or GUID with `NEWSEQUENTIALID()` if GUID is chosen)
- **API versioning:** URL-based (`/api/v1/`)
- **Authentication:** TBD (to be decided during first feature implementation)

### Configuration File Policy

-   **Only `.template` files are tracked in git.** Real `appsettings.json`, `appsettings.*.json`, `app.config`, and `web.config` files are gitignored.
-   Template files use `<PLACEHOLDER>` values for all sensitive data (connection strings, API keys, secrets).
-   When adding a new config key, update the corresponding `.template` file with a placeholder.
-   Never commit real connection strings, passwords, API keys, or tokens.
-   Developers copy `appsettings.json.template` → `appsettings.json` locally and fill in real values.

### Current Package Versions

| Package | Version | Project |
|---|---|---|
| Microsoft.AspNetCore.OpenApi | 8.0.25 | Warehouse.API |
| Swashbuckle.AspNetCore | 6.6.2 | Warehouse.API |
| NUnit | 3.13.3 | Warehouse.API.Tests |
| Microsoft.NET.Test.Sdk | 17.6.0 | Warehouse.API.Tests |
| coverlet.collector | 6.0.0 | Warehouse.API.Tests |

### Packages to Add (when first feature is implemented)

- AutoMapper.Extensions.Microsoft.DependencyInjection
- FluentValidation.AspNetCore
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools
- NLog.Web.AspNetCore
- Microsoft.Extensions.Http.Polly
- Polly
- Asp.Versioning.Mvc

---

## 0.2 Specification Alignment Gate

Before implementing any task, check alignment with existing specifications:

- `docs/core/SDD-*.md` — Core business pipeline
- `docs/domain/SDD-*.md` — Domain capabilities
- `docs/integration/SDD-*.md` — External integrations
- `docs/infrastructure/SDD-*.md` — Cross-cutting concerns

If a request is outside scope, contradicts a spec, or is ambiguous — **STOP and clarify**.

---

## 1. Project Purpose

Warehouse Management System (WMS) — a monorepo containing .NET 8 backend microservices and a Vue.js frontend (frontend to be added later). The system will manage warehouse operations including inventory, storage locations, inbound/outbound shipments, and related workflows.

**Architecture:** Monorepo with multiple backend microservices sharing common libraries, and a single Vue.js frontend application.

---

## 2. Solution Structure

```
Warehouse/
├── CLAUDE.md                                      ← You are here
├── docs/                                          ← SDD specifications
│   ├── README.md
│   ├── cross-reference-map.md
│   ├── core/                                      ← Core business pipeline specs
│   ├── domain/                                    ← Domain capability specs
│   ├── integration/                               ← External integration specs
│   ├── infrastructure/                            ← Cross-cutting concern specs
│   └── changes/
│       └── _TEMPLATE.md
├── frontend/                                      ← Vue.js app (to be added)
├── src/
│   ├── Warehouse.slnx                             ← .NET 8 solution
│   ├── Databases/
│   │   └── Warehouse.DBModel/                     ← EF Core entities, DbContext, migrations
│   │       ├── Interfaces/                        ← Repository interfaces
│   │       ├── Models/                            ← Entity models
│   │       └── Configuration/                     ← EF Core Fluent API config
│   ├── Interfaces/
│   │   ├── Warehouse.API/                         ← Controllers, middleware, DI root
│   │   │   ├── Controllers/
│   │   │   └── Middleware/
│   │   └── Warehouse.API.Tests/                   ← NUnit tests
│   ├── Warehouse.Common/                          ← Shared enums, helpers, extensions
│   │   ├── Enums/
│   │   ├── Extensions/
│   │   └── Helpers/
│   ├── Warehouse.GenericFiltering/                ← Reusable IQueryable dynamic filter library
│   ├── Warehouse.Mapping/                         ← AutoMapper profiles
│   │   └── Profiles/
│   └── Warehouse.ServiceModel/                    ← DTOs, request/response contracts
│       ├── DTOs/
│       ├── Requests/
│       └── Responses/
├── .claude/
│   └── settings.local.json
└── reporting/
```

### Project Reference Graph

```
Warehouse.API
  ├── Warehouse.DBModel
  │   └── Warehouse.Common
  ├── Warehouse.Common
  ├── Warehouse.Mapping
  │   ├── Warehouse.ServiceModel
  │   └── Warehouse.DBModel
  ├── Warehouse.ServiceModel
  └── Warehouse.GenericFiltering
      └── Warehouse.Common

Warehouse.API.Tests
  └── Warehouse.API
```

---

## 3. Domain Entities (DBModel)

No entities defined yet. This section will be populated as features are implemented.

---

## 4. API Endpoints

No endpoints defined yet. The default template endpoints exist but will be replaced.

---

## 5. Authentication & Authorization

TBD — to be decided during first feature implementation.

---

## 6. Background Processing Architecture

None yet. Will be added as needed (e.g., Hosted Services for async processing).

---

## 7. External Integrations

None yet. Planned integrations:

- ERP system (details TBD)
- Vue.js frontend (planned, not yet created)

---

## 8. Known Deviations from Persona Standards

| # | Deviation | Reason | Plan |
|---|---|---|---|
| 1 | `Nullable` enabled in `.csproj` | Default template setting | Review during first feature — may disable if not desired |
| 2 | `ImplicitUsings` enabled | Default template setting | Review during first feature — may disable for explicit control |
| 3 | No NLog configured yet | Greenfield project | Add NLog when first service is implemented |
| 4 | No FluentValidation yet | Greenfield project | Add when first endpoint with input is implemented |
| 5 | No AutoMapper yet | Greenfield project | Add when first mapping is needed |
| 6 | No Polly resilience yet | No outbound HTTP clients yet | Add with first typed HttpClient |
| 7 | No health checks yet | No services to check | Add liveness/readiness with first feature |
| 8 | No API versioning yet | No endpoints yet | Add with first controller |

---

## 9. Configuration Keys

No custom configuration yet. Will be populated as `appsettings.json` grows.

---

## 10. GenericFiltering Syntax

The `Warehouse.GenericFiltering` project is scaffolded but empty. It will provide reusable `IQueryable` dynamic filtering when implemented.

---

## 11. SDD Documentation Structure (TWO-TIER)

### Tier 1 — System Specs (`SDD-*`)

Describe current implemented behavior. Source of truth.

| Category | Folder | Purpose |
|---|---|---|
| Core | `docs/core/` | Primary workflows and entities |
| Domain | `docs/domain/` | Secondary features, metadata |
| Integration | `docs/integration/` | External system integrations |
| Infrastructure | `docs/infrastructure/` | Auth, filtering, observability |

### Tier 2 — Change Specs (`CHG-*`)

Describe proposed changes. Live in `docs/changes/`.

| Prefix | Use For |
|---|---|
| `CHG-FEAT-NNN` | New features or capabilities |
| `CHG-ENH-NNN` | Enhancements to existing behavior |
| `CHG-FIX-NNN` | Bug fixes |
| `CHG-REFAC-NNN` | Refactoring (no behavior change) |
| `CHG-DEBT-NNN` | Technical debt reduction |

---

## 12. Naming Conventions

| Item | Convention | Example |
|---|---|---|
| Controller | `{Entity}Controller` | `InventoryController` |
| Service interface | `I{Entity}Service` | `IInventoryService` |
| Service implementation | `{Entity}Service` | `InventoryService` |
| Repository interface | `I{Entity}Repository` | `IInventoryRepository` |
| Repository implementation | `{Entity}Repository` | `InventoryRepository` |
| DTO | `{Entity}Dto` | `InventoryItemDto` |
| Request model | `{Action}{Entity}Request` | `CreateInventoryItemRequest` |
| Response model | `{Entity}Response` | `InventoryItemResponse` |
| AutoMapper profile | `{Entity}MappingProfile` | `InventoryMappingProfile` |
| Validator | `{Action}{Entity}RequestValidator` | `CreateInventoryItemRequestValidator` |
| EF Core config | `{Entity}Configuration` | `InventoryItemConfiguration` |
| DB entity | `{Entity}` (no suffix) | `InventoryItem` |
| Test class | `{ClassUnderTest}Tests` | `InventoryServiceTests` |

---

## 13. Quick Reference — Key File Paths

| Purpose | Path |
|---|---|
| Solution file | `src/Warehouse.slnx` |
| API project | `src/Interfaces/Warehouse.API/` |
| API tests | `src/Interfaces/Warehouse.API.Tests/` |
| Database models | `src/Databases/Warehouse.DBModel/Models/` |
| EF Core config | `src/Databases/Warehouse.DBModel/Configuration/` |
| Repository interfaces | `src/Databases/Warehouse.DBModel/Interfaces/` |
| DTOs | `src/Warehouse.ServiceModel/DTOs/` |
| Request models | `src/Warehouse.ServiceModel/Requests/` |
| Response models | `src/Warehouse.ServiceModel/Responses/` |
| AutoMapper profiles | `src/Warehouse.Mapping/Profiles/` |
| Shared enums | `src/Warehouse.Common/Enums/` |
| Extensions | `src/Warehouse.Common/Extensions/` |
| SDD specs | `docs/` |
| Change template | `docs/changes/_TEMPLATE.md` |
