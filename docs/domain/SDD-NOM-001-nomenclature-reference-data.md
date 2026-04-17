# SDD-NOM-001 — Nomenclature Reference Data

> Status: Active
> Last updated: 2026-04-16
> Owner: TBD
> Category: Domain

## 1. Context & Scope

This spec defines the Nomenclature microservice (`Warehouse.Nomenclature.API`), a dedicated reference data service that manages standardized geographic and financial lookup tables: countries, state/provinces, cities, and currencies. These entities serve as the canonical source for address fields and currency codes used across the Warehouse system. Currently, address fields (`City`, `StateProvince`, `CountryCode`) on `CustomerAddress` and `SupplierAddress` are free-text, and `CurrencyCode` on `CustomerAccount` is a plain string. This service provides the database-backed lookup data that will eventually replace free-text entry with validated references.

**ISA-95 Conformance:** ISA-95 Part 2 -- Reference Data Management. Countries, state/provinces, cities, and currencies are master/reference data supporting Material Model (Section 7, material origin and storage site attributes), Equipment Model (Section 5, site and area geographic metadata), and Personnel Model (Section 6, personnel address data). These are cross-domain reference entities consumed by multiple ISA-95 operations domains (Inventory Operations, Procurement Operations, Fulfillment Operations, Business Partner Management). The Nomenclature service does not map to a single ISA-95 activity model function; it is a shared reference data layer analogous to Unit of Measure management.

**In scope:**

- Country management (CRUD, ISO 3166-1 alpha-2 and alpha-3 codes)
- State/province management (CRUD, hierarchically owned by country)
- City management (CRUD, hierarchically owned by state/province)
- Currency management (CRUD, ISO 4217 codes)
- Redis caching of all list endpoints with aggressive TTLs
- Database seeding from ISO 3166-1 (countries) and ISO 4217 (currencies) with feature flag gating
- Soft-delete via `IsActive` flag (no hard deletes)
- Gateway route registration for Nomenclature endpoints
- Dedicated `NomenclatureDbContext` with `nomenclature` schema
- Microservice on port 5007

**Out of scope:**

- Migration of existing free-text address/currency fields in Customers or Purchasing domains (separate change spec)
- AddressType, PhoneType, EmailType -- these remain C# enums, not database-backed lookups
- Exchange rate management (future Finance service concern)
- Postal code validation (city postal code is informational, not enforced against external databases)
- Address formatting or localization rules
- Frontend SPA views for nomenclature management (separate UI spec)

**Related specs:**

- `SDD-CUST-001` -- Customers and Accounts. `CustomerAddress.CountryCode`, `CustomerAddress.City`, `CustomerAddress.StateProvince`, and `CustomerAccount.CurrencyCode` will eventually reference Nomenclature data.
- `SDD-PURCH-001` -- Procurement Operations. `SupplierAddress.CountryCode`, `SupplierAddress.City`, `SupplierAddress.StateProvince` will eventually reference Nomenclature data.
- `SDD-AUTH-001` -- All Nomenclature endpoints require JWT authentication and permission-based authorization.
- `SDD-INFRA-001` -- Shared infrastructure library (base controller, base entity service, Redis cache, health checks, middleware pipeline).
- `SDD-INFRA-002` -- API Gateway. New routes must be added for Nomenclature endpoints.

---

## 2. Behavior

### 2.1 Country Management

#### 2.1.1 Create Country

- The system MUST support creating a country with: `iso2Code` (ISO 3166-1 alpha-2), `iso3Code` (ISO 3166-1 alpha-3), `name`, `phonePrefix`, and `isActive`.
- The system MUST enforce unique `iso2Code` across all countries (including inactive).
- The system MUST enforce unique `iso3Code` across all countries (including inactive).
- The system MUST set `IsActive = true` by default if not explicitly provided.
- The system MUST record `CreatedAtUtc` on creation.
- The system MUST return the created country with its generated integer ID.

**Edge cases:**

- Creating a country with a duplicate `iso2Code` MUST return a 409 Conflict error.
- Creating a country with a duplicate `iso3Code` MUST return a 409 Conflict error.
- Creating a country with an `iso2Code` that is not exactly 2 uppercase letters MUST return a 400 Validation error.

#### 2.1.2 Update Country

- The system MUST support updating country fields: `name`, `phonePrefix`, `isActive`.
- The system MUST NOT allow changing `iso2Code` or `iso3Code` after creation.
- The system MUST update `ModifiedAtUtc` on every update.

**Edge cases:**

- Updating a non-existent country MUST return a 404 Not Found error.
- Attempting to change `iso2Code` or `iso3Code` MUST be silently ignored (field is not accepted in the update request model).

#### 2.1.3 Deactivate Country

- The system MUST support deactivating a country by setting `IsActive = false` and recording `ModifiedAtUtc`.
- Deactivating a country SHOULD cascade deactivation to all its state/provinces and their cities.
- Deactivating an already-inactive country MUST return a 409 Conflict error.

#### 2.1.4 Reactivate Country

- The system MUST support reactivating an inactive country by setting `IsActive = true` and recording `ModifiedAtUtc`.
- Reactivating a country MUST NOT automatically reactivate its state/provinces or cities. Those must be reactivated individually.
- Reactivating an already-active country MUST return a 409 Conflict error.

#### 2.1.5 Get Country

- The system MUST support retrieving a single country by ID, including its list of state/provinces.
- Retrieving a non-existent country MUST return a 404 Not Found error.

#### 2.1.6 List Countries

- The system MUST support listing all active countries, sorted alphabetically by name.
- The system SHOULD support an optional query parameter `includeInactive` to return all countries regardless of active status.
- The list endpoint MUST return results from Redis cache when available.

### 2.2 State/Province Management

#### 2.2.1 Create State/Province

- The system MUST support creating a state/province with: `countryId`, `code`, `name`, and `isActive`.
- The system MUST enforce that `countryId` references an existing, active country. Creating a state/province for an inactive country MUST return a 409 Conflict error.
- The system MUST enforce unique `code` within the same country (two different countries MAY have state/provinces with the same code).
- The system MUST set `IsActive = true` by default if not explicitly provided.
- The system MUST record `CreatedAtUtc` on creation.

**Edge cases:**

- Creating a state/province with a non-existent `countryId` MUST return a 404 Not Found error.
- Creating a state/province with a duplicate code within the same country MUST return a 409 Conflict error.

#### 2.2.2 Update State/Province

- The system MUST support updating state/province fields: `name`, `isActive`.
- The system MUST NOT allow changing `code` or `countryId` after creation.
- The system MUST update `ModifiedAtUtc` on every update.

**Edge cases:**

- Updating a non-existent state/province MUST return a 404 Not Found error.

#### 2.2.3 Deactivate State/Province

- The system MUST support deactivating a state/province by setting `IsActive = false` and recording `ModifiedAtUtc`.
- Deactivating a state/province SHOULD cascade deactivation to all its cities.
- Deactivating an already-inactive state/province MUST return a 409 Conflict error.

#### 2.2.4 Reactivate State/Province

- The system MUST support reactivating an inactive state/province by setting `IsActive = true` and recording `ModifiedAtUtc`.
- Reactivating a state/province whose parent country is inactive MUST return a 409 Conflict error.
- Reactivating a state/province MUST NOT automatically reactivate its cities.
- Reactivating an already-active state/province MUST return a 409 Conflict error.

#### 2.2.5 List State/Provinces by Country

- The system MUST support listing state/provinces filtered by country (via `countryId` or `countryCode` query parameter).
- The system MUST return only active state/provinces by default, sorted alphabetically by name.
- The system SHOULD support an optional `includeInactive` query parameter.
- This endpoint MUST return results from Redis cache when available, keyed per country.
- Requesting state/provinces for a non-existent country MUST return a 404 Not Found error.

### 2.3 City Management

#### 2.3.1 Create City

- The system MUST support creating a city with: `stateProvinceId`, `name`, `postalCode` (optional), and `isActive`.
- The system MUST enforce that `stateProvinceId` references an existing, active state/province. Creating a city for an inactive state/province MUST return a 409 Conflict error.
- The system MUST enforce unique `name` within the same state/province (two different state/provinces MAY have cities with the same name).
- The system MUST set `IsActive = true` by default if not explicitly provided.
- The system MUST record `CreatedAtUtc` on creation.

**Edge cases:**

- Creating a city with a non-existent `stateProvinceId` MUST return a 404 Not Found error.
- Creating a city with a duplicate name within the same state/province MUST return a 409 Conflict error.

#### 2.3.2 Update City

- The system MUST support updating city fields: `name`, `postalCode`, `isActive`.
- The system MUST NOT allow changing `stateProvinceId` after creation.
- The system MUST update `ModifiedAtUtc` on every update.

**Edge cases:**

- Updating a non-existent city MUST return a 404 Not Found error.
- Updating the name to one that already exists within the same state/province MUST return a 409 Conflict error.

#### 2.3.3 Deactivate City

- The system MUST support deactivating a city by setting `IsActive = false` and recording `ModifiedAtUtc`.
- Deactivating an already-inactive city MUST return a 409 Conflict error.

#### 2.3.4 Reactivate City

- The system MUST support reactivating an inactive city by setting `IsActive = true` and recording `ModifiedAtUtc`.
- Reactivating a city whose parent state/province is inactive MUST return a 409 Conflict error.
- Reactivating an already-active city MUST return a 409 Conflict error.

#### 2.3.5 List Cities by State/Province

- The system MUST support listing cities filtered by state/province (via `stateProvinceId` query parameter).
- The system MUST return only active cities by default, sorted alphabetically by name.
- The system SHOULD support an optional `includeInactive` query parameter.
- This endpoint MUST return results from Redis cache when available, keyed per state/province.
- Requesting cities for a non-existent state/province MUST return a 404 Not Found error.

### 2.4 Currency Management

#### 2.4.1 Create Currency

- The system MUST support creating a currency with: `code` (ISO 4217), `name`, `symbol`, and `isActive`.
- The system MUST enforce unique `code` across all currencies (including inactive).
- The system MUST set `IsActive = true` by default if not explicitly provided.
- The system MUST record `CreatedAtUtc` on creation.
- The system MUST return the created currency with its generated integer ID.

**Edge cases:**

- Creating a currency with a duplicate `code` MUST return a 409 Conflict error.
- Creating a currency with a `code` that is not exactly 3 uppercase letters MUST return a 400 Validation error.

#### 2.4.2 Update Currency

- The system MUST support updating currency fields: `name`, `symbol`, `isActive`.
- The system MUST NOT allow changing `code` after creation.
- The system MUST update `ModifiedAtUtc` on every update.

**Edge cases:**

- Updating a non-existent currency MUST return a 404 Not Found error.

#### 2.4.3 Deactivate Currency

- The system MUST support deactivating a currency by setting `IsActive = false` and recording `ModifiedAtUtc`.
- Deactivating an already-inactive currency MUST return a 409 Conflict error.

#### 2.4.4 Reactivate Currency

- The system MUST support reactivating an inactive currency by setting `IsActive = true` and recording `ModifiedAtUtc`.
- Reactivating an already-active currency MUST return a 409 Conflict error.

#### 2.4.5 List Currencies

- The system MUST support listing all active currencies, sorted alphabetically by name.
- The system SHOULD support an optional query parameter `includeInactive` to return all currencies regardless of active status.
- The list endpoint MUST return results from Redis cache when available.

### 2.5 Redis Caching

- All list endpoints MUST use `IDistributedCache` for caching.
- Cache MUST be populated on first request (cache-aside pattern).
- Cache MUST be invalidated (key removed) on any write operation (create, update, deactivate, reactivate) affecting the cached entity type.
- The following cache keys and TTLs MUST be used:

| Cache Key | TTL | Invalidated By |
|---|---|---|
| `nomenclature:countries:all` | 60 minutes | Any country create/update/deactivate/reactivate |
| `nomenclature:state-provinces:country:{countryId}` | 60 minutes | Any state/province create/update/deactivate/reactivate within that country |
| `nomenclature:cities:state-province:{stateProvinceId}` | 60 minutes | Any city create/update/deactivate/reactivate within that state/province |
| `nomenclature:currencies:all` | 60 minutes | Any currency create/update/deactivate/reactivate |

**Edge cases:**

- When Redis is unavailable, the service MUST fall back to direct database queries without failing. Cache misses due to Redis downtime MUST NOT produce HTTP 500 errors.
- Country deactivation cascade (which deactivates child state/provinces and their cities) MUST invalidate all affected cache keys: the countries cache, all state/province caches for that country, and all city caches for the affected state/provinces.

### 2.6 Database Seeding

- The service MUST support database seeding for initial data population.
- Seeding MUST be gated behind feature flags:
  - `EnableNomenclatureSeeding` -- gates the entire seeder execution.
  - `EnableSeedCountries` -- gates country (and state/province, city) seeding.
  - `EnableSeedCurrencies` -- gates currency seeding.
- Country seeding MUST populate at minimum the ISO 3166-1 alpha-2 codes, alpha-3 codes, names, and phone prefixes for all internationally recognized countries.
- Currency seeding MUST populate at minimum the ISO 4217 codes, names, and symbols for all actively circulating currencies.
- Seeding MUST be idempotent: running the seeder multiple times MUST NOT create duplicate records. Existence checks MUST be based on `iso2Code` for countries and `code` for currencies.
- State/province and city seeding is OPTIONAL (MAY be implemented for a subset of countries with well-known administrative divisions).

### 2.7 API Endpoints

All endpoints MUST be versioned under `/api/v1/` and require JWT authentication.

#### Countries

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/countries` | List all active countries (cached) |
| `GET` | `/api/v1/countries/{id}` | Get country by ID with state/provinces |
| `POST` | `/api/v1/countries` | Create a new country |
| `PUT` | `/api/v1/countries/{id}` | Update country |
| `PUT` | `/api/v1/countries/{id}/deactivate` | Deactivate country |
| `PUT` | `/api/v1/countries/{id}/reactivate` | Reactivate country |

#### State/Provinces

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/state-provinces?countryId={id}` | List state/provinces by country (cached) |
| `GET` | `/api/v1/state-provinces/{id}` | Get state/province by ID |
| `POST` | `/api/v1/state-provinces` | Create a new state/province |
| `PUT` | `/api/v1/state-provinces/{id}` | Update state/province |
| `PUT` | `/api/v1/state-provinces/{id}/deactivate` | Deactivate state/province |
| `PUT` | `/api/v1/state-provinces/{id}/reactivate` | Reactivate state/province |

#### Cities

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/cities?stateProvinceId={id}` | List cities by state/province (cached) |
| `GET` | `/api/v1/cities/{id}` | Get city by ID |
| `POST` | `/api/v1/cities` | Create a new city |
| `PUT` | `/api/v1/cities/{id}` | Update city |
| `PUT` | `/api/v1/cities/{id}/deactivate` | Deactivate city |
| `PUT` | `/api/v1/cities/{id}/reactivate` | Reactivate city |

#### Currencies

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/currencies` | List all active currencies (cached) |
| `GET` | `/api/v1/currencies/{id}` | Get currency by ID |
| `POST` | `/api/v1/currencies` | Create a new currency |
| `PUT` | `/api/v1/currencies/{id}` | Update currency |
| `PUT` | `/api/v1/currencies/{id}/deactivate` | Deactivate currency |
| `PUT` | `/api/v1/currencies/{id}/reactivate` | Reactivate currency |

### 2.8 Gateway Integration

- The API Gateway MUST define a `nomenclature-cluster` with default destination `http://localhost:5007`.
- The gateway MUST define the following routes:

| Route ID | Path Pattern |
|---|---|
| `countries-route` | `/api/v1/countries/{**catch-all}` |
| `state-provinces-route` | `/api/v1/state-provinces/{**catch-all}` |
| `cities-route` | `/api/v1/cities/{**catch-all}` |
| `currencies-route` | `/api/v1/currencies/{**catch-all}` |

- The gateway health aggregation MUST include a `nomenclature-api` check probing `http://localhost:5007/health/ready`.

### 2.9 Health Checks

- The service MUST expose `/health/live` (liveness, always healthy) and `/health/ready` (readiness, checks SQL Server and Redis).
- Health check registration MUST follow the pattern established in `SDD-INFRA-001`.

---

## 3. Data Model

### 3.1 Entities

All entities reside in the `nomenclature` schema within the shared SQL Server database.

#### Country

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `int` | PK, IDENTITY | Auto-incrementing primary key |
| `Iso2Code` | `nvarchar(2)` | NOT NULL, UNIQUE | ISO 3166-1 alpha-2 code (e.g., `BG`, `US`) |
| `Iso3Code` | `nvarchar(3)` | NOT NULL, UNIQUE | ISO 3166-1 alpha-3 code (e.g., `BGR`, `USA`) |
| `Name` | `nvarchar(100)` | NOT NULL | Country name in English |
| `PhonePrefix` | `nvarchar(10)` | NULL | International dialing code (e.g., `+359`, `+1`) |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft-delete flag |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL, DEFAULT SYSUTCDATETIME() | Creation timestamp |
| `ModifiedAtUtc` | `datetime2(7)` | NULL | Last modification timestamp |

**Indexes:**

- `PK_Countries` on `Id`
- `UQ_Countries_Iso2Code` on `Iso2Code`
- `UQ_Countries_Iso3Code` on `Iso3Code`
- `IX_Countries_Name` on `Name`

#### StateProvince

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `int` | PK, IDENTITY | Auto-incrementing primary key |
| `CountryId` | `int` | NOT NULL, FK -> Countries | Parent country |
| `Code` | `nvarchar(10)` | NOT NULL | State/province code (e.g., `CA`, `SOF`) |
| `Name` | `nvarchar(100)` | NOT NULL | State/province name |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft-delete flag |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL, DEFAULT SYSUTCDATETIME() | Creation timestamp |
| `ModifiedAtUtc` | `datetime2(7)` | NULL | Last modification timestamp |

**Indexes:**

- `PK_StateProvinces` on `Id`
- `FK_StateProvinces_Countries` on `CountryId`
- `UQ_StateProvinces_CountryId_Code` on (`CountryId`, `Code`)
- `IX_StateProvinces_CountryId` on `CountryId`

#### City

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `int` | PK, IDENTITY | Auto-incrementing primary key |
| `StateProvinceId` | `int` | NOT NULL, FK -> StateProvinces | Parent state/province |
| `Name` | `nvarchar(100)` | NOT NULL | City name |
| `PostalCode` | `nvarchar(20)` | NULL | Optional postal/ZIP code |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft-delete flag |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL, DEFAULT SYSUTCDATETIME() | Creation timestamp |
| `ModifiedAtUtc` | `datetime2(7)` | NULL | Last modification timestamp |

**Indexes:**

- `PK_Cities` on `Id`
- `FK_Cities_StateProvinces` on `StateProvinceId`
- `UQ_Cities_StateProvinceId_Name` on (`StateProvinceId`, `Name`)
- `IX_Cities_StateProvinceId` on `StateProvinceId`

#### Currency

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `int` | PK, IDENTITY | Auto-incrementing primary key |
| `Code` | `nvarchar(3)` | NOT NULL, UNIQUE | ISO 4217 currency code (e.g., `USD`, `EUR`, `BGN`) |
| `Name` | `nvarchar(100)` | NOT NULL | Currency name (e.g., `US Dollar`) |
| `Symbol` | `nvarchar(5)` | NULL | Currency symbol (e.g., `$`, `EUR`, `лв.`) |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft-delete flag |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL, DEFAULT SYSUTCDATETIME() | Creation timestamp |
| `ModifiedAtUtc` | `datetime2(7)` | NULL | Last modification timestamp |

**Indexes:**

- `PK_Currencies` on `Id`
- `UQ_Currencies_Code` on `Code`

### 3.2 EF Core Configuration

- All entities MUST be configured via Fluent API (no Data Annotations for table/column mapping).
- All table and column names MUST be configured explicitly.
- The `NomenclatureDbContext` MUST use the `nomenclature` schema.
- Foreign key columns MUST be indexed.
- Navigation properties: `Country` -> `ICollection<StateProvince>`, `StateProvince` -> `ICollection<City>`, `StateProvince` -> `Country`, `City` -> `StateProvince`.

---

## 4. Validation Rules

### 4.1 Country Validation

| Field | Rule |
|---|---|
| `Iso2Code` | MUST be exactly 2 characters, uppercase letters only (`^[A-Z]{2}$`) |
| `Iso3Code` | MUST be exactly 3 characters, uppercase letters only (`^[A-Z]{3}$`) |
| `Name` | MUST be non-empty, max 100 characters |
| `PhonePrefix` | MAY be null; if provided, max 10 characters, SHOULD match pattern `^\+\d{1,9}$` |

### 4.2 State/Province Validation

| Field | Rule |
|---|---|
| `CountryId` | MUST be a positive integer referencing an existing, active country |
| `Code` | MUST be non-empty, max 10 characters |
| `Name` | MUST be non-empty, max 100 characters |

### 4.3 City Validation

| Field | Rule |
|---|---|
| `StateProvinceId` | MUST be a positive integer referencing an existing, active state/province |
| `Name` | MUST be non-empty, max 100 characters |
| `PostalCode` | MAY be null; if provided, max 20 characters |

### 4.4 Currency Validation

| Field | Rule |
|---|---|
| `Code` | MUST be exactly 3 characters, uppercase letters only (`^[A-Z]{3}$`) |
| `Name` | MUST be non-empty, max 100 characters |
| `Symbol` | MAY be null; if provided, max 5 characters |

### 4.5 Cross-Field and State-Based Rules

| Rule | Description |
|---|---|
| State/province code uniqueness | MUST be unique within the same country (`CountryId` + `Code` composite) |
| City name uniqueness | MUST be unique within the same state/province (`StateProvinceId` + `Name` composite) |
| Parent active check (state/province) | Cannot create a state/province for an inactive country |
| Parent active check (city) | Cannot create a city for an inactive state/province |
| Reactivation parent check (state/province) | Cannot reactivate a state/province if its parent country is inactive |
| Reactivation parent check (city) | Cannot reactivate a city if its parent state/province is inactive |
| Immutable codes | `Country.Iso2Code`, `Country.Iso3Code`, `StateProvince.Code`, `StateProvince.CountryId`, `City.StateProvinceId`, `Currency.Code` MUST NOT be changeable after creation |

---

## 5. Error Rules

### 5.1 Validation Errors (HTTP 400)

| Trigger | Error Type | ProblemDetails Detail |
|---|---|---|
| `Iso2Code` not matching `^[A-Z]{2}$` | Validation | "Iso2Code must be exactly 2 uppercase letters." |
| `Iso3Code` not matching `^[A-Z]{3}$` | Validation | "Iso3Code must be exactly 3 uppercase letters." |
| `Currency.Code` not matching `^[A-Z]{3}$` | Validation | "Code must be exactly 3 uppercase letters." |
| `Name` is empty or exceeds max length | Validation | "Name is required and must not exceed 100 characters." |
| `CountryId` or `StateProvinceId` is not positive | Validation | "Parent ID must be a positive integer." |
| Any other FluentValidation rule failure | Validation | FluentValidation error collection in ProblemDetails extensions |

### 5.2 Not Found Errors (HTTP 404)

| Trigger | Error Type | ProblemDetails Detail |
|---|---|---|
| Country with given ID does not exist | Not Found | "Country with ID {id} was not found." |
| State/province with given ID does not exist | Not Found | "StateProvince with ID {id} was not found." |
| City with given ID does not exist | Not Found | "City with ID {id} was not found." |
| Currency with given ID does not exist | Not Found | "Currency with ID {id} was not found." |
| `CountryId` in create state/province does not exist | Not Found | "Country with ID {countryId} was not found." |
| `StateProvinceId` in create city does not exist | Not Found | "StateProvince with ID {stateProvinceId} was not found." |
| Country for state/province list (by code) does not exist | Not Found | "Country with code '{code}' was not found." |

### 5.3 Conflict Errors (HTTP 409)

| Trigger | Error Type | ProblemDetails Detail |
|---|---|---|
| Duplicate `Iso2Code` on country create | Conflict | "A country with ISO2 code '{code}' already exists." |
| Duplicate `Iso3Code` on country create | Conflict | "A country with ISO3 code '{code}' already exists." |
| Duplicate `Currency.Code` on create | Conflict | "A currency with code '{code}' already exists." |
| Duplicate state/province code within country | Conflict | "A state/province with code '{code}' already exists in this country." |
| Duplicate city name within state/province | Conflict | "A city named '{name}' already exists in this state/province." |
| Creating state/province for inactive country | Conflict | "Cannot add a state/province to an inactive country." |
| Creating city for inactive state/province | Conflict | "Cannot add a city to an inactive state/province." |
| Deactivating an already-inactive entity | Conflict | "{Entity} with ID {id} is already inactive." |
| Reactivating an already-active entity | Conflict | "{Entity} with ID {id} is already active." |
| Reactivating state/province with inactive parent country | Conflict | "Cannot reactivate state/province because its parent country (ID {countryId}) is inactive." |
| Reactivating city with inactive parent state/province | Conflict | "Cannot reactivate city because its parent state/province (ID {stateProvinceId}) is inactive." |

### 5.4 Domain Error Mapping

All service methods MUST return `Result` or `Result<T>` from `Warehouse.Common.Models`. Controllers MUST map `Result` outcomes to HTTP responses using `BaseApiController` helpers:

| Result State | HTTP Status |
|---|---|
| `Success` | 200 OK or 201 Created |
| `NotFound` | 404 Not Found |
| `Conflict` | 409 Conflict |
| `ValidationError` | 400 Bad Request |

---

## 6. Versioning Notes

- **v1 -- Initial specification.** Defines the Nomenclature microservice as a new domain service for geographic and financial reference data. No existing implementation; this is a greenfield spec.

---

## 7. Test Plan

### 7.1 Country Service Tests

- `CreateAsync_ValidRequest_ReturnsCreatedCountry` [Unit]
- `CreateAsync_DuplicateIso2Code_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateIso3Code_ReturnsConflictError` [Unit]
- `CreateAsync_InvalidIso2CodeFormat_ReturnsValidationError` [Unit]
- `UpdateAsync_ExistingCountry_ReturnsUpdatedCountry` [Unit]
- `UpdateAsync_NonExistentCountry_ReturnsNotFoundError` [Unit]
- `DeactivateAsync_ActiveCountry_SetsInactive` [Unit]
- `DeactivateAsync_AlreadyInactive_ReturnsConflictError` [Unit]
- `DeactivateAsync_CascadesDeactivationToStateProvinces` [Unit]
- `ReactivateAsync_InactiveCountry_SetsActive` [Unit]
- `ReactivateAsync_AlreadyActive_ReturnsConflictError` [Unit]
- `ReactivateAsync_DoesNotReactivateChildren` [Unit]
- `GetByIdAsync_ExistingCountry_ReturnsCountryWithStateProvinces` [Unit]
- `GetByIdAsync_NonExistentCountry_ReturnsNotFoundError` [Unit]
- `ListAsync_ReturnsActiveCountriesSortedByName` [Unit]
- `ListAsync_IncludeInactive_ReturnsAllCountries` [Unit]

### 7.2 State/Province Service Tests

- `CreateAsync_ValidRequest_ReturnsCreatedStateProvince` [Unit]
- `CreateAsync_NonExistentCountry_ReturnsNotFoundError` [Unit]
- `CreateAsync_InactiveCountry_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateCodeWithinCountry_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateCodeDifferentCountry_Succeeds` [Unit]
- `UpdateAsync_ExistingStateProvince_ReturnsUpdatedStateProvince` [Unit]
- `UpdateAsync_NonExistentStateProvince_ReturnsNotFoundError` [Unit]
- `DeactivateAsync_ActiveStateProvince_SetsInactive` [Unit]
- `DeactivateAsync_AlreadyInactive_ReturnsConflictError` [Unit]
- `DeactivateAsync_CascadesDeactivationToCities` [Unit]
- `ReactivateAsync_InactiveStateProvince_SetsActive` [Unit]
- `ReactivateAsync_AlreadyActive_ReturnsConflictError` [Unit]
- `ReactivateAsync_InactiveParentCountry_ReturnsConflictError` [Unit]
- `ListByCountryAsync_ReturnsActiveStateProvincesSortedByName` [Unit]
- `ListByCountryAsync_NonExistentCountry_ReturnsNotFoundError` [Unit]
- `ListByCountryAsync_IncludeInactive_ReturnsAll` [Unit]

### 7.3 City Service Tests

- `CreateAsync_ValidRequest_ReturnsCreatedCity` [Unit]
- `CreateAsync_NonExistentStateProvince_ReturnsNotFoundError` [Unit]
- `CreateAsync_InactiveStateProvince_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateNameWithinStateProvince_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateNameDifferentStateProvince_Succeeds` [Unit]
- `UpdateAsync_ExistingCity_ReturnsUpdatedCity` [Unit]
- `UpdateAsync_NonExistentCity_ReturnsNotFoundError` [Unit]
- `UpdateAsync_DuplicateNameWithinStateProvince_ReturnsConflictError` [Unit]
- `DeactivateAsync_ActiveCity_SetsInactive` [Unit]
- `DeactivateAsync_AlreadyInactive_ReturnsConflictError` [Unit]
- `ReactivateAsync_InactiveCity_SetsActive` [Unit]
- `ReactivateAsync_AlreadyActive_ReturnsConflictError` [Unit]
- `ReactivateAsync_InactiveParentStateProvince_ReturnsConflictError` [Unit]
- `ListByStateProvinceAsync_ReturnsActiveCitiesSortedByName` [Unit]
- `ListByStateProvinceAsync_NonExistentStateProvince_ReturnsNotFoundError` [Unit]

### 7.4 Currency Service Tests

- `CreateAsync_ValidRequest_ReturnsCreatedCurrency` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `CreateAsync_InvalidCodeFormat_ReturnsValidationError` [Unit]
- `UpdateAsync_ExistingCurrency_ReturnsUpdatedCurrency` [Unit]
- `UpdateAsync_NonExistentCurrency_ReturnsNotFoundError` [Unit]
- `DeactivateAsync_ActiveCurrency_SetsInactive` [Unit]
- `DeactivateAsync_AlreadyInactive_ReturnsConflictError` [Unit]
- `ReactivateAsync_InactiveCurrency_SetsActive` [Unit]
- `ReactivateAsync_AlreadyActive_ReturnsConflictError` [Unit]
- `ListAsync_ReturnsActiveCurrenciesSortedByName` [Unit]
- `ListAsync_IncludeInactive_ReturnsAllCurrencies` [Unit]

### 7.5 Caching Tests

- `ListCountries_FirstCall_PopulatesCache` [Unit]
- `ListCountries_SecondCall_ServesFromCache` [Unit]
- `CreateCountry_InvalidatesCacheKey` [Unit]
- `UpdateCountry_InvalidatesCacheKey` [Unit]
- `DeactivateCountry_InvalidatesCountryAndChildCacheKeys` [Unit]
- `ListStateProvinces_CachedPerCountry` [Unit]
- `CreateStateProvince_InvalidatesCountrySpecificCacheKey` [Unit]
- `ListCities_CachedPerStateProvince` [Unit]
- `CreateCity_InvalidatesStateProvinceSpecificCacheKey` [Unit]
- `ListCurrencies_FirstCall_PopulatesCache` [Unit]
- `CreateCurrency_InvalidatesCacheKey` [Unit]
- `CacheUnavailable_FallsBackToDatabase` [Unit]

### 7.6 Seeding Tests

- `SeedAsync_EmptyDatabase_CreatesCountries` [Integration]
- `SeedAsync_ExistingData_DoesNotDuplicate` [Integration]
- `SeedAsync_FeatureFlagDisabled_SkipsSeedingEntirely` [Integration]
- `SeedAsync_CountryFlagDisabled_SkipsCountrySeedingOnly` [Integration]
- `SeedAsync_CurrencyFlagDisabled_SkipsCurrencySeedingOnly` [Integration]

### 7.7 Integration Tests (API Endpoints)

- `CreateCountry_ValidPayload_Returns201` [Integration]
- `CreateCountry_DuplicateIso2Code_Returns409` [Integration]
- `CreateCountry_InvalidPayload_Returns400ProblemDetails` [Integration]
- `CreateCountry_Unauthenticated_Returns401` [Integration]
- `GetCountries_ReturnsActiveCountries_Returns200` [Integration]
- `GetCountryById_ExistingId_Returns200WithStateProvinces` [Integration]
- `GetCountryById_NonExistentId_Returns404` [Integration]
- `CreateStateProvince_ValidPayload_Returns201` [Integration]
- `CreateStateProvince_InactiveCountry_Returns409` [Integration]
- `GetStateProvinces_ByCountryId_Returns200` [Integration]
- `CreateCity_ValidPayload_Returns201` [Integration]
- `CreateCity_InactiveStateProvince_Returns409` [Integration]
- `GetCities_ByStateProvinceId_Returns200` [Integration]
- `CreateCurrency_ValidPayload_Returns201` [Integration]
- `CreateCurrency_DuplicateCode_Returns409` [Integration]
- `GetCurrencies_ReturnsActiveCurrencies_Returns200` [Integration]
- `DeactivateCountry_ActiveCountry_Returns200` [Integration]
- `ReactivateCountry_InactiveCountry_Returns200` [Integration]

### 7.8 Validator Tests

- `CreateCountryRequestValidator_Iso2Code_MustBe2UppercaseLetters` [Unit]
- `CreateCountryRequestValidator_Iso3Code_MustBe3UppercaseLetters` [Unit]
- `CreateCountryRequestValidator_Name_Required` [Unit]
- `CreateStateProvinceRequestValidator_CountryId_MustBePositive` [Unit]
- `CreateStateProvinceRequestValidator_Code_Required` [Unit]
- `CreateCityRequestValidator_StateProvinceId_MustBePositive` [Unit]
- `CreateCityRequestValidator_Name_Required` [Unit]
- `CreateCurrencyRequestValidator_Code_MustBe3UppercaseLetters` [Unit]
- `CreateCurrencyRequestValidator_Name_Required` [Unit]
- `UpdateCountryRequestValidator_Name_Required` [Unit]
- `UpdateCurrencyRequestValidator_Name_Required` [Unit]
