# CHG-ENH-001 — Nomenclature Integration Across Consumer Domains

> Status: Implemented
> Last updated: 2026-04-16
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**

The Nomenclature microservice (`SDD-NOM-001`) provides database-backed reference data for countries, state/provinces, cities, and currencies. Currently, all address and currency fields across the Customers, Purchasing, and Fulfillment domains are free-text strings with format-only validation (e.g., 2-letter country code, 3-letter currency code). This change integrates those domains with the Nomenclature API so that geographic and financial fields are populated from standardized lookup data rather than arbitrary free-text input. The integration is frontend-driven — the SPA uses Nomenclature API endpoints to populate cascading dropdowns — while backend services add optional code-level validation against cached Nomenclature data for country and currency codes.

**Scope:**

- [x] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [x] Configuration changes

**In scope:**

- Frontend: Replace free-text inputs with cascading dropdown/autocomplete components for Country, State/Province, City, and Currency in all address and currency forms
- Frontend: Shared composable (`useNomenclature`) and API module (`frontend/src/shared/api/nomenclature.ts`) for all Nomenclature lookups
- Frontend: Shared address form component that encapsulates the cascading Country > State/Province > City pattern
- Backend: Optional validation of `CountryCode` and `CurrencyCode` against cached Nomenclature lists in Customers, Purchasing, and Fulfillment services
- Backend: DTO enrichment — add resolved `CountryName` and `CurrencyName` to address and account DTOs for display purposes
- Gateway: Nomenclature routes already defined in `SDD-NOM-001` — no additional gateway changes

**Out of scope:**

- Database schema changes — all fields remain as strings (no FK migration across schema boundaries)
- Backend validation of `City` and `StateProvince` values — these fields remain free-text on the backend because city/state names are locale-dependent and the Nomenclature hierarchy may not cover every address globally
- Migration of existing data — records created before this change retain their current free-text values
- Inventory domain (`WarehouseEntity.Address`) — single free-text address field, not structured enough for Nomenclature integration
- Exchange rate or pricing currency logic — purely a reference data display concern

**ISA-95 Conformance:** ISA-95 Part 2 — Reference Data Management. This change aligns address and currency reference data with centralized master data management, improving data quality across Personnel (addresses), Business Partner (customer/supplier addresses), and Fulfillment Operations (shipping addresses) domains.

**Related specs:**

- `SDD-NOM-001` — Nomenclature Reference Data (provides the lookup APIs consumed by this change)
- `SDD-CUST-001` — Customers and Accounts (address and currency fields affected)
- `SDD-PURCH-001` — Procurement Operations (supplier address fields affected)
- `SDD-FULF-001` — Fulfillment Operations (shipping address fields on SalesOrder and Shipment affected)
- `SDD-UI-004` — Purchasing Operations SPA (supplier address forms affected)
- `SDD-UI-005` — Fulfillment Operations SPA (SO and shipment address forms affected)
- `SDD-INFRA-001` — Shared Infrastructure Library (Redis cache used for backend validation)

---

## 2. Behavior (RFC 2119)

### 2.1 Frontend — Nomenclature Lookup Integration

#### 2.1.1 Shared Nomenclature API Module

- The system MUST provide a shared API module at `frontend/src/shared/api/nomenclature.ts` that exposes functions for fetching countries, state/provinces (by country), cities (by state/province), and currencies from the Nomenclature API.
- The system MUST cache Nomenclature API responses in memory (Pinia store or composable-level ref) for the duration of the browser session to avoid redundant network calls.
- The system SHOULD provide a `useNomenclature()` composable that manages loading states, error handling, and cascading resets (e.g., changing country clears state/province and city selections).

#### 2.1.2 Country Dropdown

- All forms that accept a country code (`CountryCode`, `ShippingCountryCode`) MUST replace the free-text input with a searchable dropdown populated from `GET /api/v1/countries`.
- The dropdown MUST display `Name` as the label and store `Iso2Code` as the value.
- The dropdown MUST be sorted alphabetically by name (server-side default).
- The dropdown MUST support type-ahead search/filtering by country name.
- When no country is selected, state/province and city dropdowns MUST be disabled and empty.

**Edge cases:**

- If the Nomenclature API is unreachable, the dropdown SHOULD fall back to a free-text input with a warning message, allowing the user to type a country code manually.
- Existing records with country codes not present in the Nomenclature database MUST still display the stored code value (with a visual indicator that it is unresolved).

#### 2.1.3 State/Province Cascading Dropdown

- All forms that accept a state/province value (`StateProvince`, `ShippingStateProvince`) MUST replace the free-text input with a searchable dropdown populated from `GET /api/v1/state-provinces?countryId={id}`, filtered by the selected country.
- The dropdown MUST display `Name` as the label and store `Name` as the value (since the backend field remains a string, not an ID reference).
- Changing the selected country MUST clear the current state/province selection and reload the state/province list for the new country.
- If the selected country has no state/provinces in the Nomenclature database, the state/province field SHOULD fall back to a free-text input to allow manual entry.

**Edge cases:**

- If a record's stored `StateProvince` value does not match any entry in the loaded state/province list, the dropdown MUST still display the stored value (as a non-matching but valid selection) and allow the user to keep or change it.

#### 2.1.4 City Cascading Dropdown

- All forms that accept a city value (`City`, `ShippingCity`) MUST replace the free-text input with a searchable dropdown populated from `GET /api/v1/cities?stateProvinceId={id}`, filtered by the selected state/province.
- The dropdown MUST display `Name` as the label and store `Name` as the value.
- Changing the selected state/province MUST clear the current city selection and reload the city list.
- If the selected state/province has no cities in the Nomenclature database, the city field SHOULD fall back to a free-text input.
- If no state/province is selected (field is optional), the city field MUST allow free-text input since there is no parent to cascade from.

**Edge cases:**

- If the Nomenclature database does not have city data for a given state/province, the field MUST fall back to free-text input. The system MUST NOT block form submission because of missing Nomenclature coverage.

#### 2.1.5 Currency Dropdown

- All forms that accept a currency code (`CurrencyCode`) MUST replace the free-text input with a searchable dropdown populated from `GET /api/v1/currencies`.
- The dropdown MUST display `Name` as the label and `Code` as a secondary label (e.g., "US Dollar (USD)"), storing `Code` as the value.
- The dropdown MUST be sorted alphabetically by name (server-side default).

**Edge cases:**

- If a record's stored `CurrencyCode` does not match any entry in the loaded currency list, the dropdown MUST still display the stored code and allow the user to keep or change it.

#### 2.1.6 Shared Address Form Component

- The system SHOULD provide a reusable address form component (e.g., `NomenclatureAddressFields.vue`) that encapsulates the cascading Country > State/Province > City pattern with all the behaviors defined in 2.1.2–2.1.4.
- This component MUST be used by: Customer address forms, Supplier address forms, Sales order shipping address forms, and Shipment address forms.
- The component MUST accept props for initial values and emit change events compatible with `v-model` binding.

#### 2.1.7 Inline Creation for Admin Users

- The Country, State/Province, and City dropdowns MUST support an "Add new" option that allows admin users to create a new entry inline without leaving the form.
- The "Add new" option MUST only be visible to users with the appropriate Nomenclature write permissions (e.g., `nomenclature:create`).
- Inline creation MUST call the Nomenclature API's create endpoints (`POST /api/v1/countries`, `POST /api/v1/state-provinces`, `POST /api/v1/cities`) and, on success, refresh the dropdown list and auto-select the newly created entry.
- If the user does not have write permissions, the dropdowns MUST only show selection from existing data (no "Add new" option).

### 2.2 Backend — Code Validation Enhancement

#### 2.2.1 Country Code Validation

- The Customers, Purchasing, and Fulfillment APIs SHOULD validate `CountryCode` and `ShippingCountryCode` values against the list of active countries from the Nomenclature database.
- Validation MUST be gated behind the `EnableNomenclatureValidation` feature flag. When the flag is `false` (or absent), validation is skipped entirely and only existing format-only rules apply.
- Validation MUST use a cached country list (via `IDistributedCache`) to avoid synchronous HTTP calls to the Nomenclature API on every request.
- The cache key MUST be `nomenclature:countries:all` (shared with the Nomenclature service's own cache).
- If the cache is empty (cold start or Redis unavailable), the validation MUST be skipped — the system MUST NOT block writes because of Nomenclature unavailability.
- If the provided `CountryCode` does not match any active country's `Iso2Code`, the service MUST return a validation error.

**Edge cases:**

- A country that was active when a record was created but later deactivated MUST NOT cause validation errors on read operations or on updates to unrelated fields. Validation applies only when the `CountryCode` field itself is being set or changed.
- If Redis is unavailable, country code validation MUST be silently skipped (fail-open).

#### 2.2.2 Currency Code Validation

- The Customers API SHOULD validate `CurrencyCode` values on `CustomerAccount` create/update against the list of active currencies from the Nomenclature database.
- Validation MUST also be gated behind the `EnableNomenclatureValidation` feature flag (same flag as country code validation).
- Validation MUST use a cached currency list (via `IDistributedCache`) with cache key `nomenclature:currencies:all`.
- The same fail-open behavior as country code validation applies: skip validation if cache is empty or Redis is unavailable.

#### 2.2.3 State/Province and City — No Backend Validation

- The system MUST NOT validate `StateProvince`, `ShippingStateProvince`, `City`, or `ShippingCity` values against Nomenclature data on the backend.
- These fields remain free-text strings on the backend. The Nomenclature integration for these fields is frontend-only (cascading dropdowns for UX improvement).
- Rationale: City and state/province names are locale-dependent, the Nomenclature database may not have complete coverage for all countries, and cross-service validation for free-text names adds unacceptable coupling and latency.

### 2.3 Backend — DTO Enrichment

#### 2.3.1 Address DTO Enrichment

- All address DTOs (`CustomerAddressDto`, `SupplierAddressDto`, `SalesOrderDetailDto`, `ShipmentDetailDto`) SHOULD include a `CountryName` field (nullable string) that resolves the stored `CountryCode` to the country's display name from the cached Nomenclature country list.
- If the country code cannot be resolved (cache miss, unknown code, or Redis unavailable), `CountryName` MUST be `null`.
- The enrichment MUST NOT block the response — it is a best-effort display enhancement.

#### 2.3.2 Account DTO Enrichment

- `CustomerAccountDto` SHOULD include a `CurrencyName` field (nullable string) that resolves the stored `CurrencyCode` to the currency's display name from the cached Nomenclature currency list.
- The same best-effort behavior applies: `CurrencyName` is `null` if resolution fails.

#### 2.3.3 Enrichment Implementation

- Each consumer service (Customers, Purchasing, Fulfillment) MUST implement a lightweight `INomenclatureResolver` service that reads from `IDistributedCache` to resolve codes to names.
- The resolver MUST NOT make HTTP calls to the Nomenclature API. It relies exclusively on the shared Redis cache that the Nomenclature service populates.
- The resolver SHOULD be registered as a singleton and used from within AutoMapper profiles or service-layer mapping logic.

### 2.4 Cache Sharing Strategy

- The Nomenclature service (`SDD-NOM-001`) populates Redis cache keys on first access and invalidates on writes.
- Consumer services (Customers, Purchasing, Fulfillment) MUST read from the same shared Redis cache keys directly — no independent local cache copies.
- Consumer services MUST NOT write to or invalidate Nomenclature cache keys — only the Nomenclature service owns those keys.
- All consumer services MUST have `ConnectionStrings:Redis` configured to point to the same Redis instance as the Nomenclature service.
- Rationale: A single shared cache avoids TTL divergence and stale data across services. The Nomenclature service is the sole cache owner; consumers are read-only.

---

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | `CountryCode` (all address forms) | If backend validation is active and cache is available, MUST match an active country's `Iso2Code` | `INVALID_COUNTRY_CODE` |
| V2 | `CurrencyCode` (CustomerAccount) | If backend validation is active and cache is available, MUST match an active currency's `Code` | `INVALID_CURRENCY_CODE` |
| V3 | `ShippingCountryCode` (SalesOrder, Shipment) | Same rule as V1 — applied to fulfillment shipping addresses | `INVALID_COUNTRY_CODE` |
| V4 | `StateProvince` / `City` (all forms) | No backend validation. Frontend provides dropdown guidance only. Existing format rules (max length) remain unchanged. | — |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | `CountryCode` does not match any active country in cache | 400 Bad Request | `INVALID_COUNTRY_CODE` | "The country code '{code}' is not recognized. Please select a valid country." |
| E2 | `CurrencyCode` does not match any active currency in cache | 400 Bad Request | `INVALID_CURRENCY_CODE` | "The currency code '{code}' is not recognized. Please select a valid currency." |
| E3 | Nomenclature cache unavailable during validation | — (skip) | — | Validation silently skipped; request proceeds with existing format-only validation |

---

## 5. Versioning Notes

**API version impact:** None — no new endpoints, no breaking changes to existing request/response contracts.

**Database migration required:** No — all fields remain as strings. No schema changes.

**Backwards compatibility:** Fully compatible.

- Existing API consumers that send valid country/currency codes will see no change.
- Existing records with free-text values that don't match Nomenclature data will continue to work.
- The new `CountryName` and `CurrencyName` DTO fields are additive (nullable) and do not break existing consumers.
- Frontend forms gracefully degrade to free-text input when Nomenclature data is unavailable.

---

## 6. Test Plan

### Unit Tests

#### Nomenclature Resolver

- [Unit] `ResolveCountryNameAsync_ValidCode_ReturnsName` — Resolver returns "Bulgaria" for "BG" when cache has data
- [Unit] `ResolveCountryNameAsync_UnknownCode_ReturnsNull` — Resolver returns null for a code not in cache
- [Unit] `ResolveCountryNameAsync_CacheUnavailable_ReturnsNull` — Resolver returns null gracefully when Redis throws
- [Unit] `ResolveCurrencyNameAsync_ValidCode_ReturnsName` — Resolver returns "US Dollar" for "USD"
- [Unit] `ResolveCurrencyNameAsync_CacheUnavailable_ReturnsNull` — Resolver returns null gracefully

#### Country Code Validation (Customers)

- [Unit] `CreateAddress_ValidCountryCode_Succeeds` — Address creation succeeds when CountryCode matches active country in cache
- [Unit] `CreateAddress_InvalidCountryCode_ReturnsValidationError` — Returns INVALID_COUNTRY_CODE when code not in cache
- [Unit] `CreateAddress_CacheUnavailable_SkipsValidation` — Address creation succeeds without Nomenclature validation when cache is empty
- [Unit] `UpdateAddress_CountryCodeNotChanged_SkipsNomenclatureValidation` — No validation when CountryCode is unchanged
- [Unit] `CreateAddress_FeatureFlagDisabled_SkipsNomenclatureValidation` — Validation skipped entirely when `EnableNomenclatureValidation` is `false`

#### Country Code Validation (Purchasing)

- [Unit] `CreateSupplierAddress_ValidCountryCode_Succeeds` — Same pattern as Customers
- [Unit] `CreateSupplierAddress_InvalidCountryCode_ReturnsValidationError` — Returns INVALID_COUNTRY_CODE
- [Unit] `CreateSupplierAddress_CacheUnavailable_SkipsValidation` — Fail-open behavior
- [Unit] `CreateSupplierAddress_FeatureFlagDisabled_SkipsNomenclatureValidation` — Validation skipped when flag is off

#### Country Code Validation (Fulfillment)

- [Unit] `CreateSalesOrder_ValidShippingCountryCode_Succeeds` — SO creation succeeds with valid country
- [Unit] `CreateSalesOrder_InvalidShippingCountryCode_ReturnsValidationError` — Returns INVALID_COUNTRY_CODE
- [Unit] `CreateSalesOrder_CacheUnavailable_SkipsValidation` — Fail-open behavior
- [Unit] `CreateSalesOrder_FeatureFlagDisabled_SkipsNomenclatureValidation` — Validation skipped when flag is off

#### Currency Code Validation (Customers)

- [Unit] `CreateAccount_ValidCurrencyCode_Succeeds` — Account creation succeeds when CurrencyCode matches active currency
- [Unit] `CreateAccount_InvalidCurrencyCode_ReturnsValidationError` — Returns INVALID_CURRENCY_CODE
- [Unit] `CreateAccount_CacheUnavailable_SkipsValidation` — Fail-open behavior
- [Unit] `CreateAccount_FeatureFlagDisabled_SkipsNomenclatureValidation` — Validation skipped when flag is off

#### DTO Enrichment

- [Unit] `CustomerAddressDto_CountryName_ResolvedFromCache` — Mapping includes resolved CountryName
- [Unit] `CustomerAddressDto_CountryName_NullWhenUnresolved` — CountryName is null for unknown codes
- [Unit] `CustomerAccountDto_CurrencyName_ResolvedFromCache` — Mapping includes resolved CurrencyName
- [Unit] `SupplierAddressDto_CountryName_ResolvedFromCache` — Same enrichment for purchasing
- [Unit] `SalesOrderDetailDto_CountryName_ResolvedFromCache` — Same enrichment for fulfillment
- [Unit] `ShipmentDetailDto_CountryName_ResolvedFromCache` — Same enrichment for fulfillment

### Integration Tests

- [Integration] `CreateCustomerAddress_WithNomenclatureCountry_Returns201WithCountryName` — End-to-end: create address, response includes CountryName
- [Integration] `CreateCustomerAccount_WithNomenclatureCurrency_Returns201WithCurrencyName` — End-to-end: create account, response includes CurrencyName
- [Integration] `CreateSupplierAddress_WithNomenclatureCountry_Returns201WithCountryName` — Same for purchasing
- [Integration] `CreateSalesOrder_WithNomenclatureShippingCountry_Returns201WithCountryName` — Same for fulfillment
- [Integration] `GetCustomerAddress_ExistingFreeTextCountryCode_ReturnsNullCountryName` — Legacy data returns null enrichment
- [Integration] `NomenclatureCacheEmpty_AllDomainsSkipValidation_Succeed` — Verify fail-open across all consumers

---

## 7. Detailed Design

### API Changes

No new endpoints. Existing endpoints are enhanced:

#### Modified Response Contracts (additive — new nullable fields)

| DTO | New Field | Type | Source |
|---|---|---|---|
| `CustomerAddressDto` | `CountryName` | `string?` | Resolved from `nomenclature:countries:all` cache by `CountryCode` |
| `CustomerAccountDto` | `CurrencyName` | `string?` | Resolved from `nomenclature:currencies:all` cache by `CurrencyCode` |
| `SupplierAddressDto` | `CountryName` | `string?` | Resolved from `nomenclature:countries:all` cache by `CountryCode` |
| `SalesOrderDetailDto` | `ShippingCountryName` | `string?` | Resolved from `nomenclature:countries:all` cache by `ShippingCountryCode` |
| `ShipmentDetailDto` | `ShippingCountryName` | `string?` | Resolved from `nomenclature:currencies:all` cache by `ShippingCountryCode` |

No changes to request contracts.

### Data Model Changes

None. All existing fields remain as strings. No new columns, no FK migrations.

### Service Layer Changes

#### New: `INomenclatureResolver` / `NomenclatureResolver`

- **Location:** `src/Warehouse.Infrastructure/Services/NomenclatureResolver.cs`
- **Purpose:** Read-only resolver that deserializes cached Nomenclature lists from `IDistributedCache` and provides `ResolveCountryNameAsync(string iso2Code)` and `ResolveCurrencyNameAsync(string currencyCode)`.
- **Registration:** `services.AddSingleton<INomenclatureResolver, NomenclatureResolver>()` — added to all consumer service `Program.cs` files.
- **Behavior:** Returns `null` on any failure (cache miss, Redis error, deserialization error). Never throws.

#### Modified: Validation in Address/Account Services

All validation calls MUST check `IFeatureManager.IsEnabledAsync(FeatureFlags.EnableNomenclatureValidation)` before invoking the resolver. When the flag is off, skip Nomenclature validation entirely.

| Service | Method | Change |
|---|---|---|
| `CustomerContactService` | `CreateAddressAsync`, `UpdateAddressAsync` | Add `CountryCode` validation against `INomenclatureResolver` (flag-gated) |
| `SupplierContactService` | `CreateAddressAsync`, `UpdateAddressAsync` | Add `CountryCode` validation against `INomenclatureResolver` (flag-gated) |
| `SalesOrderService` | `CreateAsync`, `UpdateAsync` | Add `ShippingCountryCode` validation against `INomenclatureResolver` (flag-gated) |
| `CustomerAccountService` | `CreateAsync`, `UpdateAsync` | Add `CurrencyCode` validation against `INomenclatureResolver` (flag-gated) |

#### New: Feature Flag

| Flag | Constant | Service(s) | Purpose | Default |
|---|---|---|---|---|
| `EnableNomenclatureValidation` | `FeatureFlags.EnableNomenclatureValidation` | Customers, Purchasing, Fulfillment | Gates `CountryCode` and `CurrencyCode` backend validation against Nomenclature cache | `false` |

Add the constant to `src/Warehouse.Infrastructure/Configuration/FeatureFlags.cs`. Add `FeatureManagement:EnableNomenclatureValidation` to all affected `appsettings.json.template` files. Default to `false` so validation is opt-in per environment during rollout.

#### Modified: AutoMapper Profiles or Service Mapping

| Profile / Service | Change |
|---|---|
| `CustomerMappingProfile` | Inject `INomenclatureResolver`, resolve `CountryName` and `CurrencyName` during DTO mapping |
| `PurchasingMappingProfile` | Inject `INomenclatureResolver`, resolve `CountryName` during DTO mapping |
| `FulfillmentMappingProfile` | Inject `INomenclatureResolver`, resolve `ShippingCountryName` during DTO mapping |

**Note:** AutoMapper does not natively support async resolution in profiles. The enrichment MAY be done in the service layer after mapping (call `resolver.ResolveCountryNameAsync()` and set the DTO field) rather than inside the AutoMapper profile. The implementation choice is left to the implementator.

### Frontend Changes

#### New: Shared API Module

- **File:** `frontend/src/shared/api/nomenclature.ts`
- **Exports:** `getCountries()`, `getStateProvinces(countryId)`, `getCities(stateProvinceId)`, `getCurrencies()`

#### New: Shared Composable

- **File:** `frontend/src/shared/composables/useNomenclature.ts`
- **Exports:** `useCountries()`, `useStateProvinces(countryId)`, `useCities(stateProvinceId)`, `useCurrencies()`
- Manages loading state, caching, and cascading resets.

#### New: Shared Address Fields Component

- **File:** `frontend/src/shared/components/molecules/NomenclatureAddressFields.vue`
- **Props:** `countryCode`, `stateProvince`, `city` (v-model bindings), `disabled`, `readonly`
- **Behavior:** Cascading Country > State/Province > City dropdowns with fallback to free-text

#### Modified: Address Forms (replace free-text with Nomenclature dropdowns)

| Form Component | Domain | Fields Affected |
|---|---|---|
| Customer address form (organism) | Customers | `CountryCode`, `StateProvince`, `City` |
| Supplier address form (organism) | Purchasing | `CountryCode`, `StateProvince`, `City` |
| Sales order form (organism) | Fulfillment | `ShippingCountryCode`, `ShippingStateProvince`, `ShippingCity` |
| Shipment dispatch form (organism) | Fulfillment | `ShippingCountryCode`, `ShippingStateProvince`, `ShippingCity` |

#### Modified: Account Forms (replace free-text with Nomenclature dropdown)

| Form Component | Domain | Fields Affected |
|---|---|---|
| Customer account form (organism) | Customers | `CurrencyCode` |

#### New: i18n Keys

- `shared.nomenclature.countryPlaceholder` — "Select a country..."
- `shared.nomenclature.stateProvincePlaceholder` — "Select a state/province..."
- `shared.nomenclature.cityPlaceholder` — "Select a city..."
- `shared.nomenclature.currencyPlaceholder` — "Select a currency..."
- `shared.nomenclature.loadingCountries` — "Loading countries..."
- `shared.nomenclature.loadingError` — "Could not load reference data. You may type values manually."
- `shared.nomenclature.noStateProvinces` — "No state/provinces available. Type manually."
- `shared.nomenclature.noCities` — "No cities available. Type manually."
- `shared.nomenclature.addNewCountry` — "Add new country..."
- `shared.nomenclature.addNewStateProvince` — "Add new state/province..."
- `shared.nomenclature.addNewCity` — "Add new city..."

---

## Affected System Specs

| Spec ID | Impact |
|---|---|
| `SDD-NOM-001` | No changes — this spec defines the data source. Consumer integration was explicitly out of scope and is addressed here. |
| `SDD-CUST-001` | Enhancement: `CountryCode` and `CurrencyCode` validation behavior strengthened; `CustomerAddressDto` gains `CountryName`; `CustomerAccountDto` gains `CurrencyName`. Section 2.3 (Addresses) and Section 2.2 (Accounts) affected. |
| `SDD-PURCH-001` | Enhancement: `CountryCode` validation behavior strengthened; `SupplierAddressDto` gains `CountryName`. Section 2.3 (Supplier Contact Info) affected. |
| `SDD-FULF-001` | Enhancement: `ShippingCountryCode` validation behavior strengthened; `SalesOrderDetailDto` and `ShipmentDetailDto` gain `ShippingCountryName`. Shipping address sections affected. |
| `SDD-UI-004` | Enhancement: Supplier address forms use Nomenclature dropdowns instead of free-text inputs. |
| `SDD-UI-005` | Enhancement: SO and shipment address forms use Nomenclature dropdowns instead of free-text inputs. |
| `SDD-INFRA-001` | New `INomenclatureResolver` added to shared infrastructure library. |

---

## Nomenclature Consumer Field Map

Complete mapping of all fields affected by this change:

| # | Domain | Entity | Field | Current Type | Nomenclature Entity | Nomenclature Match Field | Backend Validation | Frontend Dropdown |
|---|---|---|---|---|---|---|---|---|
| 1 | Customers | `CustomerAddress` | `CountryCode` | `nvarchar(2)` | Country | `Iso2Code` | Yes (cached) | Yes |
| 2 | Customers | `CustomerAddress` | `StateProvince` | `nvarchar(100)` | StateProvince | `Name` | No | Yes (cascading) |
| 3 | Customers | `CustomerAddress` | `City` | `nvarchar(100)` | City | `Name` | No | Yes (cascading) |
| 4 | Customers | `CustomerAccount` | `CurrencyCode` | `nvarchar(3)` | Currency | `Code` | Yes (cached) | Yes |
| 5 | Purchasing | `SupplierAddress` | `CountryCode` | `nvarchar(2)` | Country | `Iso2Code` | Yes (cached) | Yes |
| 6 | Purchasing | `SupplierAddress` | `StateProvince` | `nvarchar(100)` | StateProvince | `Name` | No | Yes (cascading) |
| 7 | Purchasing | `SupplierAddress` | `City` | `nvarchar(100)` | City | `Name` | No | Yes (cascading) |
| 8 | Fulfillment | `SalesOrder` | `ShippingCountryCode` | `nvarchar(2)` | Country | `Iso2Code` | Yes (cached) | Yes |
| 9 | Fulfillment | `SalesOrder` | `ShippingStateProvince` | `nvarchar(100)` | StateProvince | `Name` | No | Yes (cascading) |
| 10 | Fulfillment | `SalesOrder` | `ShippingCity` | `nvarchar(100)` | City | `Name` | No | Yes (cascading) |
| 11 | Fulfillment | `Shipment` | `ShippingCountryCode` | `nvarchar(2)` | Country | `Iso2Code` | Yes (cached) | Yes |
| 12 | Fulfillment | `Shipment` | `ShippingStateProvince` | `nvarchar(100)` | StateProvince | `Name` | No | Yes (cascading) |
| 13 | Fulfillment | `Shipment` | `ShippingCity` | `nvarchar(100)` | City | `Name` | No | Yes (cascading) |

---

## Migration Plan

1. **Pre-deployment:**
   - Deploy Nomenclature service (`SDD-NOM-001`) and run database seeding for countries and currencies.
   - Verify Redis cache is populated with `nomenclature:countries:all` and `nomenclature:currencies:all`.
   - No database migrations needed for consumer domains.

2. **Deployment:**
   - Deploy updated Customers, Purchasing, and Fulfillment APIs with `INomenclatureResolver` and validation enhancements.
   - Deploy updated frontend with Nomenclature dropdown components.
   - All changes are backwards-compatible — existing data and API consumers are unaffected.

3. **Post-deployment:**
   - Monitor for `INVALID_COUNTRY_CODE` and `INVALID_CURRENCY_CODE` validation errors in logs. High error rates may indicate that the Nomenclature seed data is incomplete for the client's use case.
   - Verify that DTO enrichment (`CountryName`, `CurrencyName`) is returning non-null values for common codes.

4. **Rollback:**
   - Remove `INomenclatureResolver` validation calls from consumer services. Revert to format-only validation.
   - Revert frontend to free-text inputs.
   - No data rollback needed — no schema changes were made.

---

## Resolved Questions

1. **Cache strategy:** Consumer services read from the Nomenclature service's shared Redis cache directly. No independent local cache copies — single source of truth, single TTL, no divergence risk.
2. **Inline creation:** Yes — the `NomenclatureAddressFields.vue` component supports "Add new" for admin users with `nomenclature:create` permission. Non-admin users see selection only. See §2.1.7.
3. **Feature flag:** Yes — all backend Nomenclature validation (`CountryCode`, `CurrencyCode`) is gated behind `EnableNomenclatureValidation`. Default `false` for safe rollout. See §2.2.1, §2.2.2.
