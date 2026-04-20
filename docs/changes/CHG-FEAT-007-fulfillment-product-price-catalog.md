# CHG-FEAT-007 — Fulfillment Product Price Catalog

> Status: Implemented
> Last updated: 2026-04-20
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**

Today, sales order line creation in the Fulfillment domain requires the user to enter `UnitPrice` manually on every line. This is error-prone, inconsistent across operators, and makes it impossible to centrally govern product pricing per currency. This change introduces a commercial **product price catalog** owned by the Fulfillment domain. When a sales order line is created or updated, the system resolves the current active `UnitPrice` automatically from the catalog using the sales order's `CustomerAccount.Currency` as the lookup key. The resolved price is pre-filled on the line; the user MAY still override it to support one-off discounts. If no active price exists for the (product, currency) combination, the operation is blocked and the user is directed to add a price to the catalog first.

**Scope:**

- [x] Backend API changes
- [x] Database schema changes
- [ ] Frontend changes
- [ ] Configuration changes

**In scope:**

- New `ProductPrice` entity and `fulfillment.ProductPrices` table in `Warehouse.Fulfillment.DBModel`
- New EF Core migration adding the table, unique index, and lookup index
- New RESTful CRUD endpoints under `/api/v1/product-prices` (paged list, get, create, update, delete)
- New diagnostic resolver endpoint `GET /api/v1/product-prices/resolve`
- Price resolution logic invoked during `CreateSalesOrderLine` and `UpdateSalesOrderLine`
- New RBAC permissions (`product-prices:read|create|update|delete`) and admin/operator seeding updates
- AutoMapper profile and FluentValidation rules for the new DTOs
- GenericFiltering support for the list endpoint
- Addition of `CustomerAccountId` (int NOT NULL, cross-schema plain FK to `customers.CustomerAccounts`) and `CurrencyCode` (nvarchar(3) NOT NULL) to `fulfillment.SalesOrders` so the price resolver can determine the SO's billing currency without a cross-context lookup.

**Explicitly excluded:**

- Customer-specific price overrides (all customers share catalog prices per currency)
- Quantity tier pricing (flat price only)
- Tax handling (`UnitPrice` is always excl. tax; taxes remain out of scope for the WMS)
- Currency conversion (no FX logic — the catalog must have a price in the sales order's currency or the operation is blocked)
- Historical price snapshotting on sales order lines (sales order lines already store their own `UnitPrice` column — this is not changed)
- Inventory-side product pricing (the `inventory.Products` master data stays free of pricing fields)
- Frontend UI for managing the catalog (handled in a follow-up UI change spec)
- Purchasing-side supplier price lists (separate concern, not addressed here)
- Bulk price import/export (deferred)

**ISA-95 Conformance and Deviation:**

ISA-95 Part 2 treats commercial pricing as a Level 4 (Business Planning / ERP) concern, not Level 3 (Manufacturing Operations Management). A price list is therefore not part of the standard Material Model at Level 3. Owning product prices inside the WMS is a **pragmatic deviation** driven by the absence of an ERP integration. This deviation MUST be reconciled when ERP integration arrives — at that point the catalog SHOULD become either (a) a read-through projection of ERP price master data, or (b) deprecated in favour of ISA-95 Part 4 information exchange (see `CLAUDE.md` §1.1.5). The `ProductPrice` entity is therefore NOT added to the ISA-95 terminology mapping table (`CLAUDE.md` §1.1.3) as a standard concept — it is documented as **"Extended (non-ISA-95) WMS-owned commercial data"** and is classified informally alongside the Fulfillment operations event model.

**Related specs:**

- `SDD-FULF-001` — Fulfillment Operations (sales order line creation/update flow is modified to resolve prices from the catalog)
- `SDD-CUST-001` — Customers and Accounts (the sales order's `CustomerAccount.Currency` is the lookup key)
- `SDD-INV-001` — Products and Catalog (the `ProductId` foreign key target — cross-context plain FK, no EF navigation)
- `SDD-AUTH-001` — Authentication and Authorization (new RBAC permissions and role seeding)

---

## 2. Behavior (RFC 2119)

### 2.1 Catalog Data Model

- The system MUST persist product prices in a new table `fulfillment.ProductPrices` in the `FulfillmentDbContext`.
- Each `ProductPrice` row MUST contain: `Id` (INT IDENTITY PK), `ProductId` (INT, cross-schema FK to `inventory.Products`), `CurrencyCode` (`nvarchar(3)`, ISO 4217), `UnitPrice` (`decimal(18,4)`, excl. tax), `ValidFrom` (`datetime2(7)`, nullable), `ValidTo` (`datetime2(7)`, nullable), `CreatedAt`, `CreatedByUserId`, `ModifiedAt`, `ModifiedByUserId`.
- `ProductId` MUST be stored as a plain FK column with NO EF navigation property, per the cross-context boundary policy.
- A unique index MUST exist on `(ProductId, CurrencyCode, ValidFrom)` to enable price history per product+currency while preventing exact duplicates.
- A non-unique index MUST exist on `(ProductId, CurrencyCode, ValidFrom, ValidTo)` to support the resolver lookup.
- `ValidFrom = NULL` MUST be interpreted as "effective immediately" (equivalent to `-infinity`).
- `ValidTo = NULL` MUST be interpreted as "no end date" (equivalent to `+infinity`).

### 2.2 Catalog CRUD Behavior

- The system MUST expose RESTful CRUD endpoints under `/api/v1/product-prices` (see §7 API Changes for the full list).
- `GET /api/v1/product-prices` MUST return a paged list filterable by `productId`, `currencyCode`, and `activeOnDate` via the `Warehouse.GenericFiltering` library.
- `POST /api/v1/product-prices` MUST persist a new `ProductPrice` row, set `CreatedAt = UtcNow` and `CreatedByUserId` to the authenticated user, and return `201 Created` with the full DTO.
- `PUT /api/v1/product-prices/{id}` MUST update the row's `UnitPrice`, `ValidFrom`, and `ValidTo` fields, set `ModifiedAt = UtcNow` and `ModifiedByUserId`, and return `200 OK` with the updated DTO. `ProductId` and `CurrencyCode` MUST be immutable after creation.
- `DELETE /api/v1/product-prices/{id}` MUST hard-delete the row and return `204 No Content`.
- Deleting a `ProductPrice` that is historically referenced by existing sales order lines MUST be allowed. Sales order lines store their own `UnitPrice` snapshot column and do NOT carry a foreign key to `ProductPrice`. Historical order line prices MUST remain intact.
- Overlapping validity ranges (`ValidFrom`/`ValidTo`) for the same `(ProductId, CurrencyCode)` MUST be allowed. This supports drafting a future price that takes over from an open-ended current price.

### 2.3 Price Resolution on Sales Order Line Create / Update

- When `CreateSalesOrderLine` or `UpdateSalesOrderLine` is invoked, the `SalesOrderService` MUST resolve the effective unit price as follows:
  1. Read `SalesOrder.CurrencyCode` directly (cached at SO creation from the caller-supplied `CustomerAccountId`). No cross-context lookup is required at resolve time.
  2. Query `fulfillment.ProductPrices` for rows where `ProductId = {line.ProductId}` AND `CurrencyCode = {salesOrder.CurrencyCode}` AND `(ValidFrom IS NULL OR ValidFrom <= UtcNow)` AND `(ValidTo IS NULL OR ValidTo > UtcNow)`.
  3. If multiple rows match, the resolver MUST pick the row with the most recent `ValidFrom` (treating `NULL` as oldest, i.e., lower precedence than any concrete date).
  4. If exactly one row matches (after step 3's tiebreak), its `UnitPrice` MUST be used as the resolved price.
- If the caller did NOT supply a `UnitPrice` in the request, the resolved price MUST be written to the sales order line's `UnitPrice` column.
- If the caller DID supply a `UnitPrice` in the request, the caller-supplied value MUST be preserved on the line (manual override for one-off discounts). The resolver is still invoked in this case to verify that at least one active price exists — if no active price exists, the operation MUST still be blocked per §2.4.
- If no active `ProductPrice` row exists for the `(ProductId, CurrencyCode)` pair, the operation MUST be blocked with a `400 Bad Request` ProblemDetails response using error code `FULF_PRICE_NOT_FOUND` (see §4).
- The resolver MUST use `DateTime.UtcNow` as the "now" reference point for validity checks. Callers MUST NOT be able to override this.

### 2.4 Blocking Behavior When No Price Exists

- When price resolution yields zero matches, the operation MUST fail fast before any persistence occurs (no partial sales order creation).
- The error response MUST include `productId` and `currencyCode` in the ProblemDetails `extensions` dictionary so the UI can guide the user to add the missing price.
- The system MUST NOT silently default to `UnitPrice = 0` and MUST NOT create a sales order line with an unresolved price.

### 2.5 Diagnostic Resolver Endpoint

- The system MUST expose `GET /api/v1/product-prices/resolve?productId={id}&currencyCode={code}&onDate={iso}` as a diagnostic helper that mirrors the sales-order-line resolution logic.
- The `onDate` query parameter SHOULD default to the server's `UtcNow` if omitted.
- On match, the endpoint MUST return `200 OK` with the resolved `ProductPriceDto`.
- On no match, the endpoint MUST return `404 Not Found` with a ProblemDetails body carrying error code `FULF_PRICE_NOT_FOUND`.
- This endpoint MUST apply the same resolution algorithm as §2.3 step 2–3 so that callers (UI, tests, admin tools) can preview what price would be applied for a given `(product, currency, date)` tuple.

### 2.6 RBAC Permissions

- The system MUST introduce four new permissions in the `Permissions` table:
  - `product-prices:read`
  - `product-prices:create`
  - `product-prices:update`
  - `product-prices:delete`
- The Auth database seeder MUST assign all four permissions to any role that currently owns full administrative access (the default `Administrator` / `WarehouseManager` role).
- The seeder MUST also assign `product-prices:read` to any role that currently has `sales-orders:create`, so sales order creators can view the effective price catalog.
- Each catalog endpoint MUST be guarded by `[RequirePermission("product-prices:<action>")]`.

### 2.7 Event Publishing

- Catalog CRUD operations MUST NOT publish MassTransit domain events in this iteration. The catalog is configuration/reference data rather than state-changing material operations. (Future enhancement MAY add `ProductPriceChangedEvent` once downstream subscribers exist; this is explicitly out of scope for CHG-FEAT-007.)

### 2.8 Caching

- Product price resolution MUST read directly from `FulfillmentDbContext`. Prices MUST NOT be cached in Redis in this iteration — price tables are expected to be small but writable, and stale reads would directly affect monetary correctness.

### 2.9 Sales Order Currency Capture

- The `CreateSalesOrderRequest` MUST include required fields `CustomerAccountId` (int) and `CurrencyCode` (string, ISO 4217 3-letter). The caller is responsible for supplying both (the UI already lists a customer's accounts).
- On create, the system MUST persist both fields onto the new `SalesOrder.CustomerAccountId` and `SalesOrder.CurrencyCode` columns.
- The system MUST validate that `CustomerAccountId` and `CurrencyCode` are provided; it MAY optionally later verify that the account belongs to the customer and carries that currency via an HttpClient (out of scope for this iteration — trust the caller for now).
- Once persisted, `CustomerAccountId` and `CurrencyCode` MUST be immutable on the SO header (no update endpoint MAY change them).

---

## 3. Validation Rules

| #   | Field / Input              | Rule                                                                                                                                              | Error Code                 |
| --- | -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------- |
| V1  | `ProductId`                | MUST be > 0 and MUST reference an existing row in `inventory.Products` (verified by existence check via Inventory read, or FK constraint)         | `FULF_PRICE_INVALID_PRODUCT` |
| V2  | `CurrencyCode`             | MUST be exactly 3 uppercase ASCII letters (ISO 4217 format — format-only, no semantic check in this spec)                                         | `FULF_PRICE_INVALID_CURRENCY` |
| V3  | `UnitPrice`                | MUST be >= 0 and MUST fit `decimal(18,4)` precision                                                                                               | `FULF_PRICE_INVALID_AMOUNT` |
| V4  | `ValidFrom` / `ValidTo`    | When both are provided, `ValidTo` MUST be strictly greater than `ValidFrom`                                                                       | `FULF_PRICE_INVALID_RANGE` |
| V5  | `ProductId` + `CurrencyCode` + `ValidFrom` | Tuple MUST be unique across the table (enforced by the unique index). Create/Update MUST fail with a 409 Conflict on violation                  | `FULF_PRICE_DUPLICATE`     |
| V6  | Resolver query (`§2.3–2.5`) | At least one row matching `(ProductId, CurrencyCode, now)` with valid date range MUST exist for sales-order-line create/update to proceed        | `FULF_PRICE_NOT_FOUND`     |
| V7  | `ProductId` / `CurrencyCode` on update | Enforced at the DTO shape: `UpdateProductPriceRequest` intentionally omits these fields, so the PUT payload cannot carry a different value. No runtime error code is emitted. | *(shape-enforced)* |
| V8  | `onDate` query parameter (resolver endpoint) | If provided, MUST be a valid ISO 8601 UTC datetime                                                                                      | `FULF_PRICE_INVALID_DATE`  |
| V9  | `CustomerAccountId` on `CreateSalesOrderRequest` | MUST be > 0                                                                                                                         | `SO_INVALID_CUSTOMER_ACCOUNT` |
| V10 | `CurrencyCode` on `CreateSalesOrderRequest` | MUST be 3 uppercase ASCII letters (ISO 4217 format)                                                                                      | `SO_INVALID_CURRENCY`      |

---

## 4. Error Rules

| #   | Condition                                                                             | HTTP Status | Error Code                     | Message                                                                                     |
| --- | ------------------------------------------------------------------------------------- | ----------- | ------------------------------ | ------------------------------------------------------------------------------------------- |
| E1  | `ProductId` references a non-existent product                                         | 400 Bad Request | `FULF_PRICE_INVALID_PRODUCT`   | "Product with ID {productId} does not exist."                                               |
| E2  | `CurrencyCode` is not exactly 3 uppercase letters                                     | 400 Bad Request | `FULF_PRICE_INVALID_CURRENCY`  | "Currency code '{code}' must be exactly 3 uppercase letters (ISO 4217)."                    |
| E3  | `UnitPrice` is negative or exceeds `decimal(18,4)` precision                          | 400 Bad Request | `FULF_PRICE_INVALID_AMOUNT`    | "Unit price must be zero or positive and fit within 18 digits with 4 decimal places."       |
| E4  | `ValidTo <= ValidFrom`                                                                | 400 Bad Request | `FULF_PRICE_INVALID_RANGE`     | "ValidTo must be strictly later than ValidFrom."                                            |
| E5  | Create/Update violates the unique index on `(ProductId, CurrencyCode, ValidFrom)`     | 409 Conflict | `FULF_PRICE_DUPLICATE`         | "A price for product {productId} in currency {currencyCode} with ValidFrom {validFrom} already exists." |
| E6  | No active price exists for `(ProductId, CurrencyCode)` during sales-order-line create/update (`§2.3–2.4`) | 400 Bad Request | `FULF_PRICE_NOT_FOUND`         | "No active price exists for product {productId} in currency {currencyCode}. Add a price to the catalog first." |
| E7  | *(removed)* — `ProductId` / `CurrencyCode` immutability is enforced at the DTO shape (see V7). `FULF_PRICE_IMMUTABLE_KEY` is not emitted. | — | — | — |
| E8  | Resolver endpoint: no price matches the supplied `(productId, currencyCode, onDate)`  | 404 Not Found | `FULF_PRICE_NOT_FOUND`         | "No active price found for product {productId} in currency {currencyCode} on {onDate}."    |
| E9  | Caller lacks required permission                                                      | 403 Forbidden | (standard Auth error)          | (standard ProblemDetails from `RequirePermission`)                                          |
| E10 | `Id` path parameter on `GET/PUT/DELETE /api/v1/product-prices/{id}` does not exist    | 404 Not Found | `FULF_PRICE_NOT_FOUND_BY_ID`   | "Product price with ID {id} does not exist."                                                |
| E11 | `CustomerAccountId` missing or <= 0 on `CreateSalesOrderRequest`                       | 400 Bad Request | `SO_INVALID_CUSTOMER_ACCOUNT` | "CustomerAccountId must be a positive integer."                                              |
| E12 | `CurrencyCode` missing or not 3 uppercase ASCII letters on `CreateSalesOrderRequest`   | 400 Bad Request | `SO_INVALID_CURRENCY`         | "CurrencyCode must be exactly 3 uppercase ASCII letters (ISO 4217)."                         |
| E13 | `onDate` query parameter on `/api/v1/product-prices/resolve` is not a valid ISO 8601 datetime | 400 Bad Request | `FULF_PRICE_INVALID_DATE` | "The onDate query parameter must be a valid ISO 8601 UTC datetime." |

**ProblemDetails `extensions` population (implementation-enforced):**

- `FULF_PRICE_NOT_FOUND` (E6) and `FULF_PRICE_NOT_FOUND` (E8) MUST carry `productId` and `currencyCode`; E8 also carries the resolved `onDate`.
- `FULF_PRICE_DUPLICATE` (E5) MUST carry `productId`, `currencyCode`, `validFrom`.
- `FULF_PRICE_INVALID_DATE` (E13) MUST carry the raw `onDate` the caller supplied.

All errors MUST be serialized as RFC 7807 ProblemDetails with `type`, `title`, `status`, `detail`, `instance`, and the structured `extensions` object above.

---

## 5. Versioning Notes

**API version impact:** Additive on `v1`. New endpoints introduced under the existing `/api/v1/` prefix. No existing endpoint contracts change.

**Database migration required:** Yes — see §7 Migration Notes.

**Backwards compatibility:**

- Existing sales order create/update API consumers that omit `UnitPrice` will now receive the resolved catalog price (previously the field was required on the wire). This is a **behavioural** change, but the default experience for well-formed requests remains valid (line is created with a price).
- Consumers that explicitly supply `UnitPrice` continue to work — the override path is preserved.
- **Breaking scenario:** If no active `ProductPrice` exists for `(ProductId, CurrencyCode)`, previously-successful sales-order-line create/update calls will now fail with `FULF_PRICE_NOT_FOUND` (400). Clients MUST be prepared to handle this and direct the user to create a catalog entry. Operational rollout MUST include a pre-deployment seeding task to populate prices for all actively-sold products in all active currencies.

**Versioning entries:**

- **v1 — Initial specification** (this change, 2026-04-19). Introduces the `ProductPrice` catalog, resolver logic, RBAC permissions, and diagnostic endpoint. Adds `CustomerAccountId` and `CurrencyCode` to `fulfillment.SalesOrders` (ALTER TABLE — backfilled in the Up migration from the customer's primary account).

---

## 6. Test Plan

### Unit Tests

#### `ProductPriceService` — CRUD

- [Unit] `CreateAsync_ValidRequest_ReturnsCreatedDto` — creates a price and returns the full DTO with `Id`, `CreatedAt`, `CreatedByUserId` populated
- [Unit] `CreateAsync_UnknownProductId_ReturnsInvalidProductError` — returns `FULF_PRICE_INVALID_PRODUCT` when `ProductId` does not exist
- [Unit] `CreateAsync_InvalidCurrencyCode_ReturnsInvalidCurrencyError` — returns `FULF_PRICE_INVALID_CURRENCY` for `"usd"`, `"US"`, `"USDX"`, `""`
- [Unit] `CreateAsync_NegativeUnitPrice_ReturnsInvalidAmountError` — returns `FULF_PRICE_INVALID_AMOUNT` for `-1m`
- [Unit] `CreateAsync_ValidToBeforeValidFrom_ReturnsInvalidRangeError` — returns `FULF_PRICE_INVALID_RANGE`
- [Unit] `CreateAsync_DuplicateKey_ReturnsConflict` — returns `FULF_PRICE_DUPLICATE` when `(ProductId, CurrencyCode, ValidFrom)` already exists
- [Unit] `UpdateAsync_ValidRequest_UpdatesUnitPriceAndValidity` — mutates `UnitPrice`, `ValidFrom`, `ValidTo` and sets `ModifiedAt`/`ModifiedByUserId`
- [Unit] `UpdateAsync_AttemptToChangeProductId_ReturnsImmutableKeyError` — returns `FULF_PRICE_IMMUTABLE_KEY`
- [Unit] `UpdateAsync_AttemptToChangeCurrencyCode_ReturnsImmutableKeyError` — returns `FULF_PRICE_IMMUTABLE_KEY`
- [Unit] `UpdateAsync_UnknownId_ReturnsNotFound` — returns `FULF_PRICE_NOT_FOUND_BY_ID`
- [Unit] `DeleteAsync_ExistingPrice_SoftOrHardDeletesAndReturnsSuccess` — row is removed from catalog
- [Unit] `DeleteAsync_PriceReferencedByHistoricalSalesOrderLines_StillSucceeds` — verifies no FK blocks deletion and existing SO line `UnitPrice` values are preserved
- [Unit] `DeleteAsync_UnknownId_ReturnsNotFound` — returns `FULF_PRICE_NOT_FOUND_BY_ID`
- [Unit] `GetPagedAsync_FilterByProductId_ReturnsMatchingRows` — GenericFiltering applies `ProductId` filter
- [Unit] `GetPagedAsync_FilterByCurrencyCode_ReturnsMatchingRows` — GenericFiltering applies `CurrencyCode` filter
- [Unit] `GetPagedAsync_FilterByActiveOnDate_ReturnsRowsActiveOnThatDate` — returns only rows whose validity window contains `activeOnDate`

#### `ProductPriceResolver` — resolution logic

- [Unit] `ResolveAsync_SingleActivePrice_ReturnsThatPrice` — one row with `ValidFrom = null, ValidTo = null` is returned
- [Unit] `ResolveAsync_ValidFromInPast_ValidToInFuture_ReturnsPrice` — bounded window containing `UtcNow` matches
- [Unit] `ResolveAsync_ValidFromInFuture_DoesNotMatch` — future-effective prices are excluded
- [Unit] `ResolveAsync_ValidToInPast_DoesNotMatch` — expired prices are excluded
- [Unit] `ResolveAsync_MultipleMatches_PicksMostRecentValidFrom` — of two overlapping active rows, the one with the later `ValidFrom` wins
- [Unit] `ResolveAsync_MultipleMatches_NullValidFromTreatedAsOldest` — a row with `ValidFrom = null` loses to a row with a concrete past `ValidFrom`
- [Unit] `ResolveAsync_NoMatch_ReturnsNull` — resolver surfaces the "no active price" case to callers
- [Unit] `ResolveAsync_UsesUtcNow_NotLocalTime` — boundary test proving validity is computed in UTC
- [Unit] `ResolveAsync_WrongCurrency_DoesNotMatchOtherCurrencies` — a USD price is not returned when EUR is requested

#### `SalesOrderService` — line creation / update integration with resolver

- [Unit] `CreateLineAsync_NoUnitPriceProvided_UsesResolvedCatalogPrice` — the persisted line's `UnitPrice` matches the catalog
- [Unit] `CreateLineAsync_UnitPriceProvided_PreservesCallerOverride` — caller-supplied `UnitPrice` is persisted verbatim
- [Unit] `CreateLineAsync_UnitPriceProvidedButNoActivePriceExists_StillReturnsNotFoundError` — override does not bypass the catalog existence check
- [Unit] `CreateLineAsync_NoActivePrice_ReturnsFulfPriceNotFoundError` — returns `FULF_PRICE_NOT_FOUND` and does NOT persist the line
- [Unit] `CreateLineAsync_UsesCustomerAccountCurrency_AsLookupKey` — verifies the currency is read from `SalesOrder.CurrencyCode`
- [Unit] `UpdateLineAsync_NoUnitPriceProvided_RerunsResolverAndUpdatesPrice` — update re-resolves when `UnitPrice` is omitted
- [Unit] `UpdateLineAsync_UnitPriceProvided_PreservesCallerOverride` — explicit override is kept on update
- [Unit] `UpdateLineAsync_NoActivePrice_ReturnsFulfPriceNotFoundError` — update is blocked if the catalog lost coverage since the SO was created

#### `SalesOrderService` — currency capture on create

- [Unit] `CreateSalesOrderAsync_CustomerAccountIdAndCurrencyCode_PersistedOnHeader` — both values from the request are persisted on the SO header
- [Unit] `CreateSalesOrderValidator_MissingCustomerAccountId_ReturnsInvalid` — validator fails when `CustomerAccountId <= 0`
- [Unit] `CreateSalesOrderValidator_MissingCurrencyCode_ReturnsInvalid` — validator fails when `CurrencyCode` is null/empty
- [Unit] `CreateSalesOrderValidator_LowercaseCurrencyCode_ReturnsInvalid` — validator fails when `CurrencyCode` is not 3 uppercase letters

#### `ProductPriceMappingProfile` / DTO mapping

- [Unit] `ProductPriceMappingProfile_IsValid` — AutoMapper profile configuration passes `AssertConfigurationIsValid`
- [Unit] `ProductPriceDto_Map_PopulatesAllFields` — entity → DTO carries `Id`, `ProductId`, `CurrencyCode`, `UnitPrice`, `ValidFrom`, `ValidTo`, audit columns

#### `CreateProductPriceRequestValidator` / `UpdateProductPriceRequestValidator`

- [Unit] `CreateValidator_RejectsLowercaseCurrency` — FluentValidation rule fails for `"usd"`
- [Unit] `CreateValidator_RejectsNegativeUnitPrice` — fails for `-0.01m`
- [Unit] `CreateValidator_RejectsValidToBeforeOrEqualValidFrom` — fails for equal and earlier values
- [Unit] `CreateValidator_AcceptsNullValidFromAndValidTo` — open-ended price is valid
- [Unit] `UpdateValidator_RejectsProductIdChange` — rejects payload that attempts to change `ProductId`
- [Unit] `UpdateValidator_RejectsCurrencyCodeChange` — rejects payload that attempts to change `CurrencyCode`

### Integration Tests

- [Integration] `PostProductPrice_ValidPayload_Returns201WithDto` — end-to-end create via HTTP
- [Integration] `PostProductPrice_Duplicate_Returns409ProblemDetails` — unique index violation surfaces as `FULF_PRICE_DUPLICATE`
- [Integration] `PostProductPrice_InvalidPayload_Returns400ProblemDetails` — FluentValidation errors surface as ProblemDetails
- [Integration] `PostProductPrice_WithoutPermission_Returns403` — caller without `product-prices:create`
- [Integration] `GetProductPrices_FilterByProductIdAndCurrency_ReturnsPagedSubset` — GenericFiltering end-to-end
- [Integration] `GetProductPriceById_ExistingId_Returns200WithDto`
- [Integration] `GetProductPriceById_UnknownId_Returns404`
- [Integration] `PutProductPrice_AttemptToChangeProductId_Returns400ImmutableKey`
- [Integration] `DeleteProductPrice_ExistingId_Returns204`
- [Integration] `DeleteProductPrice_ReferencedByHistoricalOrderLines_Returns204AndLinesUnaffected` — create SO+line, delete catalog entry, confirm SO line still holds its snapshot `UnitPrice`
- [Integration] `GetResolve_ActivePriceExists_Returns200WithDto` — diagnostic endpoint happy path
- [Integration] `GetResolve_NoActivePrice_Returns404WithFulfPriceNotFound` — diagnostic endpoint miss path
- [Integration] `GetResolve_OverlappingPrices_ReturnsMostRecentValidFrom` — tiebreak verified through the HTTP surface
- [Integration] `CreateSalesOrderLine_NoUnitPrice_UsesCatalogPrice` — SO line endpoint resolves and persists the catalog price
- [Integration] `CreateSalesOrderLine_NoActivePrice_Returns400FulfPriceNotFound` — caller receives the blocking error end-to-end
- [Integration] `CreateSalesOrderLine_UnitPriceProvided_PreservesOverride` — explicit override path end-to-end
- [Integration] `UpdateSalesOrderLine_NoActivePrice_Returns400FulfPriceNotFound` — update path blocks the same way
- [Integration] `PostSalesOrder_ValidCustomerAccountAndCurrency_Returns201` — end-to-end SO creation with new required fields
- [Integration] `PostSalesOrder_MissingCustomerAccountId_Returns400` — validator rejects missing `CustomerAccountId`
- [Integration] `SeedPermissions_AdministratorRole_HasAllProductPricePermissions` — migration/seeder verification
- [Integration] `SeedPermissions_SalesOrderCreatorRoles_HaveReadPermission` — cascading read grant verified

---

## 7. Detailed Design

### API Changes

| Method | Route                                         | Permission                             | Success Response       |
| ------ | --------------------------------------------- | -------------------------------------- | ---------------------- |
| GET    | `/api/v1/product-prices`                      | `product-prices:read`      | `200 OK` paged list    |
| GET    | `/api/v1/product-prices/{id}`                 | `product-prices:read`      | `200 OK` DTO           |
| GET    | `/api/v1/product-prices/resolve`              | `product-prices:read`      | `200 OK` DTO / `404`   |
| POST   | `/api/v1/product-prices`                      | `product-prices:create`    | `201 Created` DTO      |
| PUT    | `/api/v1/product-prices/{id}`                 | `product-prices:update`    | `200 OK` DTO           |
| DELETE | `/api/v1/product-prices/{id}`                 | `product-prices:delete`    | `204 No Content`       |

#### Request / Response DTOs (new — in `Warehouse.ServiceModel`)

| DTO / Request / Response               | Fields                                                                                                                                          |
| -------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `ProductPriceDto`                      | `Id`, `ProductId`, `CurrencyCode`, `UnitPrice`, `ValidFrom?`, `ValidTo?`, `CreatedAt`, `CreatedByUserId`, `ModifiedAt?`, `ModifiedByUserId?`    |
| `CreateProductPriceRequest`            | `ProductId`, `CurrencyCode`, `UnitPrice`, `ValidFrom?`, `ValidTo?`                                                                              |
| `UpdateProductPriceRequest`            | `UnitPrice`, `ValidFrom?`, `ValidTo?` (no `ProductId` / `CurrencyCode`)                                                                         |
| `ResolveProductPriceQuery` (implicit)  | query params: `productId`, `currencyCode`, `onDate?`                                                                                             |

### Data Model Changes

New EF entity in `Warehouse.Fulfillment.DBModel/Models/ProductPrice.cs` with fluent configuration in a new `ProductPriceConfiguration` class. The table, indexes, and precision MUST be configured explicitly via Fluent API — no Data Annotations and no reliance on EF conventions.

| Column             | SQL Type          | Nullable | Notes                                        |
| ------------------ | ----------------- | -------- | -------------------------------------------- |
| `Id`               | `int IDENTITY`    | No       | PK `PK_ProductPrices`                        |
| `ProductId`        | `int`             | No       | Cross-context plain FK (no EF navigation)    |
| `CurrencyCode`     | `nvarchar(3)`     | No       |                                              |
| `UnitPrice`        | `decimal(18,4)`   | No       |                                              |
| `ValidFrom`        | `datetime2(7)`    | Yes      |                                              |
| `ValidTo`          | `datetime2(7)`    | Yes      |                                              |
| `CreatedAt`        | `datetime2(7)`    | No       | Default `SYSUTCDATETIME()`                   |
| `CreatedByUserId`  | `int`             | No       | FK to `auth.Users` (plain column)            |
| `ModifiedAt`       | `datetime2(7)`    | Yes      |                                              |
| `ModifiedByUserId` | `int`             | Yes      | FK to `auth.Users` (plain column)            |

| Index                                                                | Kind       |
| -------------------------------------------------------------------- | ---------- |
| `UX_ProductPrices_ProductId_CurrencyCode_ValidFrom`                  | Unique     |
| `IX_ProductPrices_ProductId_CurrencyCode_ValidFrom_ValidTo`          | Non-unique |

#### Changes to `fulfillment.SalesOrders`

| Column             | SQL Type          | Nullable | Notes                                                                                                 |
| ------------------ | ----------------- | -------- | ----------------------------------------------------------------------------------------------------- |
| `CustomerAccountId`| `int`             | No       | Cross-context plain FK to `customers.CustomerAccounts` (no EF navigation). Immutable after SO creation. |
| `CurrencyCode`     | `nvarchar(3)`     | No       | ISO 4217 currency code cached at SO creation. Immutable after creation.                                |

| Index                               | Kind       |
| ----------------------------------- | ---------- |
| `IX_SalesOrders_CustomerAccountId`  | Non-unique |

### Service Layer Changes

#### New: `IProductPriceService` / `ProductPriceService`

- **Location:** `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/ProductPriceService.cs`
- **Responsibilities:** CRUD orchestration, validation delegation to FluentValidation, mapping via AutoMapper, audit stamp population, unique-index violation translation to `FULF_PRICE_DUPLICATE`.

#### New: `IProductPriceResolver` / `ProductPriceResolver`

- **Location:** `src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Services/ProductPriceResolver.cs`
- **Purpose:** Pure read-side service encapsulating the resolution algorithm (§2.3 steps 2–3). Used by both `SalesOrderService` and the diagnostic `/resolve` endpoint.
- **Signature (conceptual):** `Task<ProductPrice?> ResolveAsync(int productId, string currencyCode, DateTime onUtc, CancellationToken ct)`.

#### Modified: `SalesOrderService`

- Inject `IProductPriceResolver`.
- In `CreateLineAsync` and `UpdateLineAsync`, call the resolver before persisting; block with `FULF_PRICE_NOT_FOUND` when no match; apply resolved price when the caller omitted `UnitPrice`; keep caller override when supplied.

#### New: AutoMapper Profile

- **Location:** `src/Warehouse.Mapping/Profiles/ProductPriceMappingProfile.cs`
- Maps `ProductPrice` ↔ `ProductPriceDto`, `CreateProductPriceRequest` → `ProductPrice`, `UpdateProductPriceRequest` → `ProductPrice` (ignoring `ProductId` and `CurrencyCode`).

#### New: FluentValidation Validators

- `CreateProductPriceRequestValidator`
- `UpdateProductPriceRequestValidator`

Both live alongside existing Fulfillment validators and are registered via the standard assembly scan.

#### RBAC Seeding

- Update the Auth database seeder (`DatabaseSeeder`) to insert the four new permissions (gated by the existing `EnableDatabaseSeeding` and `EnableSeedDefaultAdmin` feature flags — the catalog permissions inherit the same gating as other seeded RBAC data).

### Migration Notes

A **new EF Core migration MUST be added** under `src/Databases/Warehouse.Fulfillment.DBModel/Migrations/` to create the `fulfillment.ProductPrices` table with the columns, PK, unique index, and lookup index described above. The migration SQL is intentionally NOT included in this spec and is to be produced during implementation (Phase 2) using `dotnet ef migrations add`. Follow the project's migration naming convention (`v{X.Y.Z}_{ShortDescription}.sql` if a hand-crafted SQL script is also required for the ops pipeline).

The migration MUST create the `fulfillment.ProductPrices` table AND MUST ALTER `fulfillment.SalesOrders` to add `CustomerAccountId int NOT NULL` and `CurrencyCode nvarchar(3) NOT NULL`. For pre-existing `SalesOrders` rows, the migration MUST backfill `CustomerAccountId` using the customer's primary account (`customers.CustomerAccounts WHERE CustomerId = SalesOrders.CustomerId AND IsPrimary = 1 AND IsDeleted = 0` — pick the lowest `Id` on ties) and `CurrencyCode` from that same account. If any existing SO has no primary account, the migration MUST fail loudly with a descriptive error so ops can backfill manually before re-running. Since the backfill requires cross-schema reads, the migration `Up()` MUST use raw SQL (`migrationBuilder.Sql`) for the data backfill step.

---

## Affected System Specs

| Spec ID       | Impact                                                                                                                                                                                                                                                                                                |
| ------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SDD-FULF-001` | **Enhancement.** Sales order line create/update flow is modified to resolve `UnitPrice` from the new catalog. Behavior section on `POST /salesorders/{id}/lines` and `PUT /salesorders/{id}/lines/{lineId}` must be updated to describe auto-resolution, override, and `FULF_PRICE_NOT_FOUND` error. A new sub-section introducing the Product Price Catalog entity and CRUD endpoints is required. |
| `SDD-CUST-001` | **No behaviour change.** Spec continues to own `CustomerAccount.Currency` semantics. The catalog merely consumes that field as a lookup key. A cross-reference note MAY be added.                                                                                                                  |
| `SDD-INV-001`  | **No behaviour change.** `inventory.Products` is unchanged. A note stating "pricing is owned by Fulfillment and referenced via plain cross-context FK" is recommended.                                                                                                                             |
| `SDD-AUTH-001` | **Enhancement.** Permission catalog gains four new `product-prices:*` permissions. Default admin role seeding section must be updated accordingly.                                                                                                                                     |

---

## Migration Plan

1. **Pre-deployment:**
   - Implement the EF migration and review the generated SQL for naming conventions (`PK_ProductPrices`, `UX_ProductPrices_*`, `IX_ProductPrices_*`).
   - Seed the catalog with at least one active price per `(product, currency)` pair for all currencies referenced by existing `CustomerAccount` rows. This is mandatory to avoid breaking existing sales-order workflows on rollout (see §5 breaking scenario).
   - Confirm the Auth seeder has been updated so admin roles can immediately manage the catalog after deploy.

2. **Deployment:**
   - Apply the migration (`dotnet ef database update` or equivalent ops process).
   - Deploy the updated Fulfillment API and Auth seeder.
   - Execute the catalog-seeding script.

3. **Post-deployment:**
   - Smoke test the resolver endpoint for a known `(product, currency)` pair.
   - Create one sales order line without a `UnitPrice` and verify the catalog price is applied.
   - Monitor logs for `FULF_PRICE_NOT_FOUND` occurrences; each occurrence indicates a missing catalog entry that ops should backfill.

4. **Rollback:**
   - Revert Fulfillment API assemblies to the previous version. The new table remains in place (harmless) but is unused.
   - If full rollback is required, run the `Down` migration to drop `fulfillment.ProductPrices` and re-seed permissions to remove the four new entries.

---

## Open Questions

- [ ] Should the diagnostic resolver endpoint be surfaced under `/api/v1/product-prices/resolve` (current proposal) or under a more generic `/api/v1/pricing/resolve`? Current decision: keep it under `/product-prices` to avoid introducing a second noun; revisit if additional pricing concerns arrive.
- [ ] Should the catalog emit a `ProductPriceChangedEvent` on create/update/delete? Current decision: no downstream subscriber exists yet, so out of scope. Revisit when the event-log UI or reporting domain needs price-change traceability.
- [ ] When ERP integration is introduced (ISA-95 Part 4), should the catalog become read-only (populated by ERP sync) or be deprecated entirely in favour of ERP-owned pricing? To be decided at ERP integration time; deviation flagged in §1.
