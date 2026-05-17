# CHG-FIX-002 — Sales Order create flow: align frontend with CHG-FEAT-007 contract and add SO-number preview

> Status: Draft
> Last updated: 2026-05-16
> Owner: TBD
> Priority: P0

## 1. Context & Scope

**Problem.** `POST /api/v1/sales-orders` is unusable from the SPA. CHG-FEAT-007 §2.9 added two `required` fields to `CreateSalesOrderRequest` on the backend (`CustomerAccountId`, `CurrencyCode`) and the validator rejects requests missing them with `SO_INVALID_CUSTOMER_ACCOUNT` / `SO_INVALID_CURRENCY` (HTTP 400). The frontend `SalesOrderFormDialog` was never updated: it has no UI to pick a customer account, derives no currency, and does not send either field on submit. Every create attempt 400s before reaching the DB.

**Secondary issues found in the same form:**

- The customers/warehouses/products/carriers dropdowns are loaded with `Promise.all` and an empty `catch {}`, so any single failure silently empties **all four** dropdowns.
- The same fan-out requests `pageSize=1000`. The Customers search endpoint enforces a FluentValidation `PageSize` cap of 1..100 (`SearchCustomersRequestValidator`) and returns HTTP 400 `INVALID_PAGE_SIZE`, so the customer dropdown is permanently empty. Other services silently clamp via `PaginationParams.EffectivePageSize`, masking the same anti-pattern.
- `requestedShipDate` is serialized via `Date.toISOString().split('T')[0]`, which converts local-midnight to UTC and shifts the date back a day for users east of UTC.
- `populateForm` runs in a non-`immediate` watch on `visible`, so the `SalesOrderCreatePage` (which mounts with `visible = ref(true)` from the start) never resets the form on first paint.
- There is no SO-number preview in the create form. `GenerateOrderNumberAsync` is `private` inside `SalesOrderService` and not exposed.
- The shipping address is free-typed even when the picked customer already has saved addresses in `CustomerDetailDto.addresses`. Operators retype identical addresses and risk inconsistencies between order and customer master data.

**Why P0.** Sales order creation is a primary fulfillment workflow and is currently broken end-to-end from the SPA. CHG-FEAT-007 backend changes shipped without their corresponding frontend changes.

**Scope:**

- [x] Backend code (new endpoint + service method)
- [ ] Database changes
- [x] Frontend code (types, API, form, i18n)
- [ ] Configuration changes

**In scope:**

- New public `ISalesOrderService.GetNextOrderNumberAsync` returning the deterministic preview of the next SO number, refactored from the existing private `GenerateOrderNumberAsync`.
- New `GET /api/v1/sales-orders/next-number` endpoint requiring `sales-orders:create`.
- Frontend `CreateSalesOrderRequest` type extended with `customerAccountId` and `currencyCode`.
- Frontend `SalesOrderFormDialog`:
  - Customer account dropdown (loaded from `GET /api/v1/customers/{id}/accounts` after the user picks a customer).
  - Currency code derived from the selected account (read-only display).
  - Read-only SO-number field shown in create mode, populated from `/sales-orders/next-number`.
  - `Promise.allSettled` for dropdown fan-out so a single failure does not blank the form.
  - `pageSize` capped at `100` on all four reference-data calls, matching `PaginationParams.MaxPageSize` and the customers validator.
  - Local-date serialization for `requestedShipDate`.
  - Page-mode populate on first mount.
  - Customer-address picker above the shipping fields: after a customer is chosen, the form lists that customer's saved addresses (from `GET /api/v1/customers/{id}`) and lets the operator copy one into the shipping fields. Manual edits remain possible.
- i18n keys for the new labels (`account`, `currency`, `orderNumber`, `selectCustomerFirst`, `useCustomerAddress`, `useCustomerAddressHint`, `noAddressesForCustomer`, etc.) in `en.ts` and `bg.ts`.

**Explicitly excluded:**

- Concurrency hardening of `GenerateOrderNumberAsync` (counter race under simultaneous creates). That work is tracked under CHG-REFAC-014 and is out of scope here.
- Edit-mode flow changes — the SO header update endpoint already does not require `customerAccountId`/`currencyCode` (immutable after creation per CHG-FEAT-007 §2.9).
- Backend changes to the validator: it already enforces the right contract.

**Related:**

- CHG-FEAT-007 — Fulfillment Product Price Catalog (introduced the now-required fields).
- SDD-FULF-001 — Fulfillment Operations.
- SDD-UI-005 — Fulfillment SPA.

---

## 2. Behavior (RFC 2119)

### 2.1 Backend — next-number preview

- The system MUST expose `GET /api/v1/sales-orders/next-number` returning a JSON string body containing the next SO number that would be assigned to a sales order created right now.
- The endpoint MUST require the `sales-orders:create` permission (same gate as create).
- The returned value MUST follow the same `SO-{YYYYMMDD}-{NNNN}` format as `GenerateOrderNumberAsync` and MUST use UTC for the date prefix.
- The endpoint MUST be a pure read (no DB writes, no side effects).
- The endpoint SHOULD be implemented by extracting `GenerateOrderNumberAsync` into a public `GetNextOrderNumberAsync` on `ISalesOrderService` and delegating from both the new endpoint and the existing `CreateAsync` flow to that single method.

### 2.2 Frontend — request contract

- The frontend `CreateSalesOrderRequest` type MUST include `customerAccountId: number` and `currencyCode: string`.
- The form MUST send both fields on submit; both MUST be derived from a customer-account choice the user makes in the form (no defaulting to magic values).

### 2.3 Frontend — account / currency selection

- After the user picks a customer in the create dialog, the form MUST fetch that customer's accounts via `GET /api/v1/customers/{id}/accounts`.
- The form MUST present the loaded accounts in a dropdown labelled "Account", showing the currency code and (when available) the description.
- When a customer has exactly one account, the form SHOULD pre-select it.
- When a customer has a primary account (`isPrimary === true`), the form SHOULD pre-select that one.
- Selecting an account MUST set `form.customerAccountId` to the account's ID and `form.currencyCode` to the account's `currencyCode`.
- The currency MUST be displayed read-only next to the account selector.
- Changing the customer MUST clear the previously selected account and currency and reload the new customer's accounts.
- The form MUST block submission when no account is selected, with a localized error message.

### 2.4 Frontend — SO-number preview

- In create mode the form MUST display a read-only "SO Number" field populated from `GET /api/v1/sales-orders/next-number`.
- The preview MUST be loaded on dialog open / page mount and MUST NOT block other dropdowns from loading.
- A failure to load the preview MUST NOT block the form (the field stays empty; the backend assigns the real number on save).

### 2.5 Frontend — robustness fixes

- Reference-data fan-out in `loadDropdowns` MUST use `Promise.allSettled`. A single rejected promise MUST NOT empty unrelated dropdowns.
- Reference-data fan-out MUST request `pageSize` no greater than `100` (matches `PaginationParams.MaxPageSize` and the customers validator). Requesting more than 100 against `/customers` returns HTTP 400.
- `requestedShipDate` MUST be serialized using local-time `getFullYear() / getMonth() / getDate()` so the wire date matches what the user picked, regardless of timezone.
- `populateForm` MUST run on first mount when the form opens with `visible === true` (i.e., the page-mode case). This is achieved by either an `{ immediate: true }` watcher option or an explicit call from `onMounted`.

### 2.6 Frontend — customer-address picker

- After the user picks a customer, the form MUST display a "Use Customer Address" selector listing addresses returned in `CustomerDetailDto.addresses`.
- Each option MUST show the full street/city/state/country/postal-code line plus the `addressType` label (e.g., Shipping, Billing, Both).
- The selector MUST be hidden when no customer is selected, and SHOULD show a localized "no addresses" placeholder when the picked customer has zero addresses.
- When the customer has a `Shipping` or `Both` address with `isDefault === true`, the form SHOULD pre-select that address on customer change and pre-fill the shipping fields with its values. When the customer has exactly one `Shipping`/`Both` address, the form SHOULD pre-select it.
- Selecting an address MUST overwrite `shippingStreetLine1`, `shippingStreetLine2`, `shippingCity`, `shippingStateProvince`, `shippingPostalCode`, and `shippingCountryCode` with the address values. The fields MUST remain editable so operators can override per-order details (e.g., one-time delivery to a different site).
- The picker MUST NOT modify the billing address; the existing "Same as shipping" checkbox is the single billing-mirror control.
- Changing the customer MUST clear the previously selected address ID and reload the new customer's address list.
- The picker MUST NOT call any new backend endpoint — it reuses the existing `GET /api/v1/customers/{id}` call already triggered for accounts loading.

---

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | `customerAccountId` | Greater than 0 | `SO_INVALID_CUSTOMER_ACCOUNT` (existing — backend) |
| V2 | `currencyCode` | 3 uppercase ASCII letters (ISO 4217) | `SO_INVALID_CURRENCY` (existing — backend) |
| V3 | Frontend submit | Account selected (i.e. `customerAccountId` non-null) | UI-only (`common.required` localized message) |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Surface |
|---|---|---|---|---|
| E1 | `next-number` called without `sales-orders:create` permission | 403 | (handled by `[RequirePermission]`) | Auth.API authorization handler |
| E2 | `next-number` called by anonymous caller | 401 | (handled by `[Authorize]`) | ASP.NET auth pipeline |
| E3 | Frontend `searchCustomers` / `searchWarehouses` / `searchProducts` / `searchCarriers` rejects | n/a | n/a | Per-promise log; surviving dropdowns still render. |
| E4 | Frontend `getNextSalesOrderNumber` rejects | n/a | n/a | Empty preview; create still works (backend assigns real number). |
| E5 | Frontend `getCustomerById` rejects after picking a customer | n/a | n/a | Account dropdown stays empty; user notified via existing error toast. |

---

## 5. Versioning Notes

**API version impact:** Additive — one new GET endpoint under the existing `v1` route. No breaking change to `POST /api/v1/sales-orders` (its required contract is unchanged from CHG-FEAT-007 — this fix simply makes the frontend conform).

**Database migration required:** No.

**Backwards compatibility:** Backend additive (new endpoint, no contract change). Frontend type widens `CreateSalesOrderRequest` with two new required fields — this is technically a breaking type change for any other caller, but the only caller is `SalesOrderFormDialog`, which is updated in the same change.

**Versioning entries:**

- **v1 — Initial fix (2026-04-28).** Introduces `GetNextOrderNumberAsync`, the new GET endpoint, the frontend account/currency UI, the SO-number preview, and the dropdown / date / page-mode robustness fixes.

---

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] GetNextOrderNumberAsync_NoExistingSOs_ReturnsSequence0001` — verifies date prefix + `0001` suffix when the day has no SOs.
- [ ] `[Unit] GetNextOrderNumberAsync_WithThreeExistingSOsToday_ReturnsSequence0004` — counter increments correctly off existing same-day SOs.
- [ ] `[Unit] GetNextOrderNumberAsync_DoesNotMutateDatabase` — assertion that no `SaveChangesAsync` happens.
- [ ] `[Unit] CreateAsync_StillUsesGetNextOrderNumberInternally` — regression-safety: existing creation flow keeps generating the same format after the refactor.

### Integration Tests

- [ ] `[Integration] GET /sales-orders/next-number returns 200 with a valid SO-formatted string for an authenticated user with sales-orders:create.`
- [ ] `[Integration] GET /sales-orders/next-number returns 403 for an authenticated user without sales-orders:create.`
- [ ] `[Integration] GET /sales-orders/next-number returns 401 anonymously.`

### Manual / E2E

- [ ] Open the create dialog in the SPA → SO Number field shows a preview of form `SO-YYYYMMDD-NNNN`.
- [ ] Pick a customer with multiple accounts → account dropdown lists them; currency updates with the selection.
- [ ] Pick a customer with one account → that account is auto-selected; currency is auto-populated.
- [ ] Submit the form → `customerAccountId` and `currencyCode` are present in the network payload; the request returns 201.
- [ ] Pick a future ship date east of UTC → the date sent to the backend equals the calendar date the user picked.
- [ ] Stop the customers service and reload the dialog → warehouse / product / carrier dropdowns still populate.
- [ ] Open `/fulfillment/sales-orders/create` in page-mode → form is empty (not stale data) and the SO-number preview loads.
- [ ] Open the create dialog → customer dropdown is populated (verifies `pageSize ≤ 100` against the `/customers` validator).
- [ ] Pick a customer with multiple saved addresses → "Use Customer Address" dropdown lists them, and the default Shipping/Both address is pre-applied.
- [ ] Pick a different address from the list → all six shipping fields update to the selected address values.
- [ ] Edit a shipping field after picking an address → the field accepts edits (no read-only state).
- [ ] Pick a customer with no addresses → "Use Customer Address" placeholder reads "no saved addresses"; shipping fields are blank and editable.
- [ ] Change customer → previously selected address ID resets; the new customer's addresses load.

---

## 7. Detailed Design

### 7.1 Backend

**`Warehouse.Fulfillment.API.Interfaces.ISalesOrderService`** — add:

```csharp
/// <summary>Returns the next SO number that would be assigned to a sales order created now (preview, no DB writes).</summary>
Task<string> GetNextOrderNumberAsync(CancellationToken cancellationToken);
```

**`Warehouse.Fulfillment.API.Services.SalesOrderService`** — promote the existing `private async Task<string> GenerateOrderNumberAsync(CancellationToken)` to `public async Task<string> GetNextOrderNumberAsync(CancellationToken)` (interface method) and call it from `CreateAsync` instead of the old name.

**`Warehouse.Fulfillment.API.Controllers.SalesOrdersController`** — add:

```csharp
[HttpGet("next-number")]
[RequirePermission("sales-orders:create")]
[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
public async Task<IActionResult> GetNextOrderNumberAsync(CancellationToken cancellationToken)
{
    string next = await _soService.GetNextOrderNumberAsync(cancellationToken);
    return Ok(next);
}
```

### 7.2 Frontend

- `frontend/src/features/fulfillment/types/fulfillment.ts` — extend `CreateSalesOrderRequest` with `customerAccountId: number; currencyCode: string;`.
- `frontend/src/features/fulfillment/api/sales-orders.ts` — add `getNextSalesOrderNumber(): Promise<string>` returning `apiClient.get<string>('/sales-orders/next-number').then(r => r.data)`.
- `frontend/src/features/fulfillment/components/organisms/SalesOrderFormDialog.vue`:
  - Local state `accounts: CustomerAccountDto[]`, `accountsLoading`, `nextOrderNumber: string`, `customerAddresses: CustomerAddressDto[]`, `selectedShippingAddressId: number | null`.
  - `onCustomerChanged` calls `getCustomerById` via `loadCustomerData` and populates both accounts and `customerAddresses`; pre-selects primary or sole account, and the default Shipping/Both address.
  - `applyAddressToShipping(address)` writes the six shipping fields from a `CustomerAddressDto`.
  - `onShippingAddressPicked(id)` reuses `applyAddressToShipping` when an operator picks from the dropdown.
  - New v-text-field showing `nextOrderNumber` (read-only) in create mode.
  - New v-select for accounts (`form.customerAccountId`); currency shown read-only next to it (`form.currencyCode`).
  - New v-select for shipping address (`selectedShippingAddressId`) shown only when `form.customerId` is set, listing `customerAddresses` with `shippingAddressItemTitle` formatter; clearable; manual fields remain editable.
  - `loadDropdowns` rewritten with `Promise.allSettled` and `pageSize: 100` on all four calls.
  - `onShipDatePicked` writes `${y}-${MM}-${dd}` from the picked Date's local components.
  - Watcher `watch(visible, populateForm, { immediate: true })`.
- i18n keys: `salesOrders.form.account`, `salesOrders.form.currency`, `salesOrders.form.orderNumber`, `salesOrders.form.selectCustomerFirst`, `salesOrders.form.noAccountsForCustomer`, `salesOrders.form.useCustomerAddress`, `salesOrders.form.useCustomerAddressHint`, `salesOrders.form.noAddressesForCustomer`.

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-FULF-001 | Adds the `GET /api/v1/sales-orders/next-number` endpoint as a behavior the spec should enumerate (preview-only read). The existing create rules already match. |
| SDD-UI-005 | Fulfillment SPA — sales-order create form gains an account selector, currency display, and SO-number preview. |

## Migration Plan

1. **Pre-deployment:** none.
2. **Deployment:** ship backend (new endpoint) and frontend (form updates) together. Backend is additive; frontend depends on it for the SO-number preview but degrades gracefully if absent (preview field stays empty).
3. **Post-deployment:** verify the create flow end-to-end against staging. Confirm `customerAccountId` and `currencyCode` are present in `POST /api/v1/sales-orders` request bodies.
4. **Rollback:** revert the frontend (form falls back to the broken state) and the backend controller change. No data migration needed.

## Open Questions

- None. Pricing-catalog coverage for the selected `currencyCode` × line products is already enforced by CHG-FEAT-007 §2.3 and surfaces as `FULF_PRICE_NOT_FOUND` (400) — reused here unchanged.
