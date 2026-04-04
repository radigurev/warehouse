# Warehouse — Project Instructions

> Last updated: 2026-04-05

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
- **ORM:** EF Core with separate DbContext per domain (AuthDbContext, CustomersDbContext)
- **Validation:** FluentValidation
- **PK strategy:** INT IDENTITY (or GUID with `NEWSEQUENTIALID()` if GUID is chosen)
- **API versioning:** URL-based (`/api/v1/`)
- **Authentication:** JWT-based with RBAC (implemented in Warehouse.Auth.API)
- **Frontend:** Vue.js 3 SPA (Single Page Application) with Composition API, TypeScript, Vue Router, Pinia state management

### Configuration File Policy

-   **Only `.template` files are tracked in git.** Real `appsettings.json`, `appsettings.*.json`, `app.config`, and `web.config` files are gitignored.
-   Template files use `<PLACEHOLDER>` values for all sensitive data (connection strings, API keys, secrets).
-   When adding a new config key, update the corresponding `.template` file with a placeholder.
-   Never commit real connection strings, passwords, API keys, or tokens.
-   Developers copy `appsettings.json.template` → `appsettings.json` locally and fill in real values.

### Current Package Versions

| Package | Version | Projects |
|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.12 | Auth.DBModel, Customers.DBModel |
| Microsoft.EntityFrameworkCore.Design | 8.0.12 | Auth.API, Customers.API |
| AutoMapper.Extensions.Microsoft.DependencyInjection | 12.0.1 | Auth.API, Customers.API, Mapping |
| FluentValidation.AspNetCore | 11.3.0 | Auth.API, Customers.API |
| Asp.Versioning.Mvc | 8.1.0 | Auth.API, Customers.API |
| Swashbuckle.AspNetCore | 6.6.2 | Auth.API, Customers.API |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.12 | Auth.API, Customers.API |
| NLog.Web.AspNetCore | 5.4.0 | Auth.API, Customers.API |
| AspNetCore.HealthChecks.SqlServer | 8.0.2 | Auth.API, Customers.API |
| BCrypt.Net-Next | 4.0.3 | Auth.API |
| Polly | 8.5.2 | Customers.API |
| Microsoft.Extensions.Http.Polly | 8.0.12 | Customers.API |
| NUnit | 3.13.3 | Auth.API.Tests, Customers.API.Tests |
| FluentAssertions | 6.12.2 | Auth.API.Tests, Customers.API.Tests |
| Moq | 4.20.72 | Customers.API.Tests |

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

Warehouse Management System (WMS) — a monorepo containing .NET 8 backend microservices and a Vue.js Single Page Application (SPA) frontend. The system will manage warehouse operations including inventory, storage locations, inbound/outbound shipments, and related workflows.

**Architecture:** Monorepo with multiple backend microservices sharing common libraries, and a single Vue.js SPA frontend that consumes the backend REST APIs.

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
├── frontend/                                      ← Vue.js 3 SPA (Single Page Application)
│   └── src/
│       ├── app/                                   ← App shell (main.ts, App.vue, router/)
│       ├── features/                              ← Feature domains
│       │   └── auth/                              ← Auth feature (api, types, views, composables, organisms)
│       └── shared/                                ← Cross-feature (stores, atoms, molecules, templates, i18n)
├── src/
│   ├── Warehouse.slnx                             ← .NET 8 solution
│   ├── Databases/
│   │   ├── Warehouse.Auth.DBModel/                ← Auth EF Core entities, AuthDbContext, migrations
│   │   │   └── Models/                            ← Auth entity models (User, Role, Permission, etc.)
│   │   └── Warehouse.Customers.DBModel/           ← Customers EF Core entities, CustomersDbContext, migrations
│   │       └── Models/                            ← Customer entity models (Customer, Account, etc.)
│   ├── Interfaces/
│   │   ├── Auth/                                  ← Auth domain
│   │   │   ├── Warehouse.Auth.API/                ← Controllers, middleware, DI root
│   │   │   └── Warehouse.Auth.API.Tests/          ← NUnit tests
│   │   └── Customers/                             ← Customers domain
│   │       ├── Warehouse.Customers.API/           ← Controllers, middleware, DI root
│   │       └── Warehouse.Customers.API.Tests/     ← NUnit tests
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
Warehouse.Auth.API
  ├── Warehouse.Auth.DBModel
  │   └── Warehouse.Common
  ├── Warehouse.Common
  ├── Warehouse.Mapping
  │   ├── Warehouse.ServiceModel
  │   ├── Warehouse.Auth.DBModel
  │   └── Warehouse.Customers.DBModel
  ├── Warehouse.ServiceModel
  └── Warehouse.GenericFiltering
      └── Warehouse.Common

Warehouse.Customers.API
  ├── Warehouse.Customers.DBModel
  │   └── Warehouse.Common
  ├── Warehouse.Common
  ├── Warehouse.Mapping
  ├── Warehouse.ServiceModel
  └── Warehouse.GenericFiltering
      └── Warehouse.Common

Warehouse.Auth.API.Tests
  └── Warehouse.Auth.API

Warehouse.Customers.API.Tests
  ├── Warehouse.Customers.API
  └── Warehouse.Mapping
```

---

## 3. Domain Entities

### Auth Domain (`Warehouse.Auth.DBModel` — `AuthDbContext`)

| Entity | Schema | Table | Description |
|---|---|---|---|
| User | auth | Users | Application users |
| Role | auth | Roles | Authorization roles |
| Permission | auth | Permissions | Granular resource-action permissions |
| UserRole | auth | UserRoles | User-to-role join table |
| RolePermission | auth | RolePermissions | Role-to-permission join table |
| RefreshToken | auth | RefreshTokens | JWT refresh token storage |
| UserActionLog | auth | UserActionLogs | Immutable audit trail |

### Customers Domain (`Warehouse.Customers.DBModel` — `CustomersDbContext`)

| Entity | Schema | Table | Description |
|---|---|---|---|
| Customer | customers | Customers | Core customer entity |
| CustomerCategory | customers | CustomerCategories | Customer classification |
| CustomerAccount | customers | CustomerAccounts | Multi-currency accounts |
| CustomerAddress | customers | CustomerAddresses | Physical addresses |
| CustomerPhone | customers | CustomerPhones | Phone numbers |
| CustomerEmail | customers | CustomerEmails | Email addresses |

**Note:** `Customer.CreatedByUserId` and `Customer.ModifiedByUserId` are plain FK columns referencing `auth.Users` — no EF navigation property (cross-context boundary).

---

## 4. API Endpoints

### Auth API (`Warehouse.Auth.API` — port 5001)

See `docs/infrastructure/SDD-AUTH-001-authentication-and-authorization.md` for full endpoint documentation.

### Customers API (`Warehouse.Customers.API` — port 5003)

See `docs/domain/SDD-CUST-001-customers-and-accounts.md` for full endpoint documentation (27 endpoints).

---

## 5. Authentication & Authorization

JWT-based authentication with RBAC (Role-Based Access Control). Implemented in `Warehouse.Auth.API`.

- **Access tokens:** JWT signed with HMAC-SHA256, 30-minute expiration
- **Refresh tokens:** Opaque random strings stored in DB, 7-day expiration, rotation on use
- **Authorization:** Permission-based via `[RequirePermission("resource:action")]` attribute
- **Password hashing:** BCrypt
- **Audit:** Immutable `UserActionLogs` table for all user management events
- **Spec:** `docs/infrastructure/SDD-AUTH-001-authentication-and-authorization.md`

---

## 6. Background Processing Architecture

None yet. Will be added as needed (e.g., Hosted Services for async processing).

---

## 7. External Integrations

None yet. Planned integrations:

- ERP system (details TBD)
- Vue.js 3 SPA frontend (consuming backend REST APIs)

---

## 8. Known Deviations from Persona Standards

| # | Deviation | Reason | Plan |
|---|---|---|---|
| 1 | `Nullable` enabled in `.csproj` | Default template setting | Review — may disable if not desired |
| 2 | `ImplicitUsings` enabled | Default template setting | Review — may disable for explicit control |
| 3 | Polly only on Customers.API | Auth.API has no outbound HTTP clients | Add Polly to Auth.API when it needs typed HttpClients |
| 4 | Mapping project references both DBModel projects | Shared AutoMapper profiles | Split into per-domain mapping if isolation becomes critical |

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
| Auth API | `src/Interfaces/Auth/Warehouse.Auth.API/` |
| Auth API tests | `src/Interfaces/Auth/Warehouse.Auth.API.Tests/` |
| Auth DB models | `src/Databases/Warehouse.Auth.DBModel/Models/` |
| Auth DbContext | `src/Databases/Warehouse.Auth.DBModel/AuthDbContext.cs` |
| Customers API | `src/Interfaces/Customers/Warehouse.Customers.API/` |
| Customers API tests | `src/Interfaces/Customers/Warehouse.Customers.API.Tests/` |
| Customers DB models | `src/Databases/Warehouse.Customers.DBModel/Models/` |
| Customers DbContext | `src/Databases/Warehouse.Customers.DBModel/CustomersDbContext.cs` |
| DTOs | `src/Warehouse.ServiceModel/DTOs/` |
| Request models | `src/Warehouse.ServiceModel/Requests/` |
| Response models | `src/Warehouse.ServiceModel/Responses/` |
| AutoMapper profiles | `src/Warehouse.Mapping/Profiles/` |
| Shared enums | `src/Warehouse.Common/Enums/` |
| Extensions | `src/Warehouse.Common/Extensions/` |
| SDD specs | `docs/` |
| Change template | `docs/changes/_TEMPLATE.md` |
