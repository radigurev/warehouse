# SDD-PURCH-001 — Procurement Operations

> Status: Draft
> Last updated: 2026-04-06
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Procurement Operations domain for the Warehouse system. It covers supplier management, purchase order lifecycle, goods receiving, receiving inspection, supplier returns, and purchase history. The Purchasing service is the first microservice to introduce inter-service messaging via MassTransit/RabbitMQ, publishing events that the Inventory service consumes to create stock movements and batches.

**ISA-95 Conformance:** Conforms to ISA-95 Part 2, Section 7 -- Material Model (Material Lot linkage to receipt events) and ISA-95 Part 3 -- Procurement Operations Activity Model (Procurement Request, Material Receipt, Material Shipment -- return). The Supplier entity maps to ISA-95 "Business Partner (supplier)" per Part 2. All state-changing operations produce immutable operations event records per ISA-95 Operations Event Model.

**Service:** `Warehouse.Purchasing.API`
**Schema:** `purchasing`
**Port:** 5004
**DbContext:** `PurchasingDbContext`

**In scope:**
- Supplier CRUD with soft-delete and reactivation
- Supplier categories, contact information (addresses, phones, emails), and payment terms
- Purchase order lifecycle with status machine (Draft, Confirmed, PartiallyReceived, Received, Closed, Cancelled)
- Purchase order lines referencing products from the Inventory domain
- Goods receiving against purchase order lines with batch auto-creation
- Receiving inspection (accept, reject, quarantine) per goods receipt line
- Supplier returns with stock movement generation
- Purchase event history (immutable audit trail)
- MassTransit event publishing for cross-service integration

**Out of scope:**
- Supplier pricing tiers and contract management (future enhancement)
- Automated reorder point calculation (future enhancement, depends on demand forecasting)
- Supplier performance scoring and analytics (future enhancement)
- Supplier self-service portal (no public-facing endpoints)
- Three-way matching (PO, receipt, invoice) -- deferred to Finance service
- Quality inspection workflows beyond basic accept/reject/quarantine (see future Quality service)
- Frontend views (covered by separate UI spec when implemented)

**Related specs:**
- `SDD-AUTH-001` -- All endpoints require JWT authentication and permission-based authorization.
- `SDD-INV-001` -- Products referenced by PO lines and goods receipt lines.
- `SDD-INV-002` -- Stock movements and batches created by goods receiving and supplier return events.
- `SDD-INV-003` -- Warehouses and storage locations referenced by goods receipt lines.
- `SDD-CUST-001` -- Supplier entity model mirrors Customer entity structure (categories, contacts).

---

## 2. Behavior

### 2.1 Supplier Management

#### 2.1.1 Create Supplier

- The system MUST support creating a supplier with a code, name, optional tax ID, optional category, optional payment terms (days), and optional notes.
- The system MUST generate a unique supplier code if one is not provided. Generated codes MUST follow the format `SUPP-{sequential number padded to 6 digits}` (e.g., `SUPP-000001`).
- The system MUST enforce unique supplier codes across all suppliers (including soft-deleted).
- The system MUST enforce unique tax IDs across all active suppliers. Two soft-deleted suppliers MAY share a tax ID.
- The system MUST set `IsActive = true` by default on creation.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.
- The system MUST return the created supplier with its generated ID and code.

**Edge cases:**
- Creating a supplier with a duplicate code MUST return a 409 Conflict error.
- Creating a supplier with a duplicate tax ID (among active suppliers) MUST return a 409 Conflict error.
- Creating a supplier with an invalid (non-existent) category ID MUST return a 400 Validation error.

#### 2.1.2 Update Supplier

- The system MUST support updating supplier fields: name, tax ID, category, payment terms, and notes.
- The system MUST NOT allow changing the supplier code after creation.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on every update.
- Updating a soft-deleted supplier MUST return a 404 Not Found error.

**Edge cases:**
- Updating the tax ID to one that already belongs to another active supplier MUST return a 409 Conflict error.
- Updating a supplier with a non-existent category ID MUST return a 400 Validation error.

#### 2.1.3 Get Supplier

- The system MUST support retrieving a single supplier by ID, including nested contact information and category.
- Retrieving a soft-deleted supplier MUST return a 404 Not Found error.

#### 2.1.4 Search Suppliers

- The system MUST support paginated listing of suppliers with configurable page size and page number.
- The system MUST support filtering by: name (contains), code (exact or starts-with), tax ID (exact), category ID, and active status.
- The system MUST support sorting by name, code, and creation date.
- The system MUST default to sorting by name ascending when no sort is specified.
- The system MUST exclude soft-deleted suppliers from search results by default.
- The system SHOULD support a query parameter to include soft-deleted suppliers for administrative purposes.

#### 2.1.5 Deactivate Supplier (Soft Delete)

- The system MUST support soft-deleting a supplier by setting `IsDeleted = true` and `DeletedAtUtc = current UTC time`.
- Soft-deleted suppliers MUST NOT appear in standard queries or searches.
- The system MUST set `IsActive = false` when a supplier is soft-deleted.
- The system SHOULD prevent deactivation if the supplier has open purchase orders (status Draft or Confirmed). This SHOULD return a 409 Conflict error.

**Edge cases:**
- Deactivating an already soft-deleted supplier MUST return a 404 Not Found error.
- Deactivating a supplier with open purchase orders SHOULD return a 409 Conflict error.

#### 2.1.6 Reactivate Supplier

- The system MUST support reactivating a soft-deleted supplier by setting `IsDeleted = false`, `DeletedAtUtc = null`, and `IsActive = true`.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on reactivation.
- Reactivating a supplier whose code now conflicts with another active supplier MUST return a 409 Conflict error with code `DUPLICATE_SUPPLIER_CODE`.
- Reactivating a supplier whose tax ID now conflicts with another active supplier MUST return a 409 Conflict error with code `DUPLICATE_TAX_ID`.

### 2.2 Supplier Categories

#### 2.2.1 Create Category

- The system MUST support creating supplier categories with a name and optional description.
- The system MUST enforce unique category names.

#### 2.2.2 Update Category

- The system MUST support updating category name and description.
- The system MUST enforce unique category names on update.

#### 2.2.3 Delete Category

- The system MUST prevent deletion of a category that is assigned to one or more suppliers. This MUST return a 409 Conflict error with the count of affected suppliers.

#### 2.2.4 List Categories

- The system MUST support listing all supplier categories.
- The system SHOULD support pagination.

### 2.3 Supplier Contact Information

Contact information for suppliers follows the same patterns as Customer contacts (see `SDD-CUST-001`).

#### 2.3.1 Addresses

- The system MUST support creating addresses for a supplier with: address type (Billing, Shipping, Both), street line 1, street line 2 (optional), city, state/province (optional), postal code, and country code.
- The system MUST mark the first address of each type as the default for that type.
- The system MUST support updating all address fields except the supplier association.
- The system MUST support deleting an address. If the deleted address was the default, the system SHOULD automatically promote the next most recently created address of the same type to default.
- The system MUST support listing all addresses for a supplier, with optional filtering by address type.

#### 2.3.2 Phones

- The system MUST support creating phone entries for a supplier with: phone type (Mobile, Landline, Fax), phone number, and extension (optional).
- The system MUST mark the first phone as the primary phone.
- The system MUST support updating phone type, number, extension, and primary flag. When setting a phone as primary, the system MUST unset the previous primary phone.
- The system MUST support deleting a phone entry. If the deleted phone was primary, the system SHOULD automatically promote the next phone to primary.
- The system MUST support listing all phones for a supplier.

#### 2.3.3 Emails

- The system MUST support creating email entries for a supplier with: email type (General, Billing, Support), email address.
- The system MUST enforce unique email addresses per supplier.
- The system MUST mark the first email as the primary email.
- The system MUST support updating email type, address, and primary flag. When setting an email as primary, the system MUST unset the previous primary email. The system MUST enforce uniqueness of the email address within the same supplier on update.
- The system MUST support deleting an email entry. If the deleted email was primary, the system SHOULD automatically promote the next email to primary.
- The system MUST support listing all emails for a supplier.

### 2.4 Purchase Orders

#### 2.4.1 Create Purchase Order

- The system MUST support creating a purchase order with a supplier ID, expected delivery date (optional), destination warehouse ID, optional notes, and one or more lines.
- The system MUST auto-generate a unique PO number following the format `PO-{YYYYMMDD}-{sequential number padded to 4 digits}` (e.g., `PO-20260406-0001`). The sequential counter resets daily.
- The system MUST set the initial status to `Draft`.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.
- The system MUST return the created PO with its generated ID and PO number.

**Edge cases:**
- Creating a PO for a non-existent or soft-deleted supplier MUST return a 404 Not Found error with code `SUPPLIER_NOT_FOUND`.
- Creating a PO for an inactive (deactivated) supplier MUST return a 409 Conflict error with code `SUPPLIER_INACTIVE`.
- Creating a PO with no lines MUST return a 400 Validation error.
- Creating a PO with an invalid warehouse ID MUST return a 400 Validation error with code `INVALID_WAREHOUSE`.

#### 2.4.2 Add Purchase Order Line

- Each PO line MUST specify a product ID, ordered quantity, unit price, and optional notes.
- The system MUST validate that the product ID references an existing, non-deleted product (cross-service: Inventory).
- The system MUST prevent duplicate product entries within the same PO. This MUST return a 409 Conflict error with code `DUPLICATE_PO_LINE`.
- Lines can only be added when the PO is in `Draft` status. Adding a line to a non-Draft PO MUST return a 409 Conflict error with code `PO_NOT_EDITABLE`.

#### 2.4.3 Update Purchase Order Line

- The system MUST support updating line quantity, unit price, and notes.
- Lines can only be updated when the PO is in `Draft` status.

#### 2.4.4 Remove Purchase Order Line

- The system MUST support removing a line from a PO.
- Lines can only be removed when the PO is in `Draft` status.
- The system MUST prevent removing the last line of a PO. This MUST return a 409 Conflict error with code `PO_MUST_HAVE_LINES`.

#### 2.4.5 Update Purchase Order Header

- The system MUST support updating PO header fields: expected delivery date, destination warehouse ID, and notes.
- Header updates are only permitted when the PO is in `Draft` status. Attempting to update a non-Draft PO MUST return a 409 Conflict error with code `PO_NOT_EDITABLE`.

#### 2.4.6 Get Purchase Order

- The system MUST support retrieving a single PO by ID, including supplier details, all lines with product details, and receiving progress per line.
- Receiving progress MUST show: ordered quantity, received quantity (sum of accepted goods receipt quantities for that line), and remaining quantity.

#### 2.4.7 Search Purchase Orders

- The system MUST support paginated listing of POs with configurable page size and page number.
- The system MUST support filtering by: supplier ID, status, PO number (exact or starts-with), date range (created date), destination warehouse ID.
- The system MUST support sorting by PO number, created date, expected delivery date, and supplier name.
- The system MUST default to sorting by created date descending (newest first).

#### 2.4.8 Purchase Order Status Machine

The PO status machine defines the following valid transitions:

| From | To | Trigger | Rules |
|---|---|---|---|
| `Draft` | `Confirmed` | Manual confirm action | PO must have at least one line |
| `Draft` | `Cancelled` | Manual cancel action | No goods received against this PO |
| `Confirmed` | `PartiallyReceived` | Automatic on first goods receipt | At least one line has received quantity > 0 but not all lines are fully received |
| `Confirmed` | `Cancelled` | Manual cancel action | No goods received against this PO |
| `PartiallyReceived` | `Received` | Automatic when all lines are fully received | Every line's received quantity >= ordered quantity |
| `PartiallyReceived` | `Closed` | Manual close action | Force-close a partially received PO (e.g., supplier cannot fulfill remainder) |
| `Received` | `Closed` | Manual close action | Administrative close after all receiving is complete |

- The system MUST enforce the status machine transitions. Any invalid transition MUST return a 409 Conflict error with code `INVALID_PO_STATUS_TRANSITION`.
- The system MUST record `ModifiedAtUtc` and `ModifiedByUserId` on every status change.
- The system MUST record `ConfirmedAtUtc` and `ConfirmedByUserId` when transitioning to `Confirmed`.
- The system MUST record `ClosedAtUtc` and `ClosedByUserId` when transitioning to `Closed`.
- Transitions to `PartiallyReceived` and `Received` are automatic and MUST NOT be triggered directly by API calls. These transitions MUST occur as a side effect of goods receiving.
- Cancellation of a PO that has received goods MUST return a 409 Conflict error with code `PO_HAS_RECEIPTS`.

**Edge cases:**
- Confirming a PO with no lines MUST return a 409 Conflict error.
- Attempting to cancel a PartiallyReceived PO MUST return a 409 Conflict error with code `INVALID_PO_STATUS_TRANSITION`.
- Closing an already-closed PO MUST return a 409 Conflict error with code `PO_ALREADY_CLOSED`.

### 2.5 Goods Receiving

#### 2.5.1 Create Goods Receipt

- The system MUST support creating a goods receipt against a purchase order.
- A goods receipt MUST reference a PO that is in `Confirmed` or `PartiallyReceived` status. Receiving against a PO in any other status MUST return a 409 Conflict error with code `PO_NOT_RECEIVABLE`.
- The goods receipt MUST specify a receiving warehouse ID and receiving location ID (optional).
- The system MUST auto-generate a unique receipt number following the format `GR-{YYYYMMDD}-{sequential number padded to 4 digits}` (e.g., `GR-20260406-0001`).
- The system MUST record `CreatedAtUtc`, `CreatedByUserId`, and `ReceivedAtUtc` on creation.

#### 2.5.2 Goods Receipt Lines

- Each goods receipt line MUST reference a PO line ID, specify the received quantity, and optionally specify a batch number, manufacturing date, and expiry date.
- The received quantity MUST be greater than zero.
- The system MUST prevent receiving more than the remaining quantity on a PO line (ordered quantity minus previously received quantity). Over-receiving MUST return a 409 Conflict error with code `OVER_RECEIPT`.
- The system SHOULD allow a configurable tolerance percentage for over-receipt (default: 0%). When tolerance is configured, receiving up to the tolerance percentage above the remaining quantity is permitted.
- The system MUST set the initial inspection status of each receipt line to `Pending`.

#### 2.5.3 Goods Receipt Completion

- When a goods receipt is completed (all lines entered and confirmed), the system MUST publish a `GoodsReceiptCompletedEvent` via MassTransit.
- The event MUST contain: receipt ID, receipt number, PO ID, PO number, warehouse ID, location ID, and for each accepted line: product ID, quantity, batch number, manufacturing date, expiry date.
- The Inventory service (consumer) MUST create a `StockMovement` with reason code `Receipt`, reference type `PurchaseOrder`, and reference ID set to the PO ID.
- The Inventory service (consumer) MUST create or update a `Batch` record linking the material lot to the receipt event when a batch number is provided on the receipt line.
- After successful receipt completion, the system MUST update the PO status according to the status machine rules (to `PartiallyReceived` or `Received`).

**Edge cases:**
- Creating a goods receipt for a cancelled PO MUST return a 409 Conflict error.
- Creating a goods receipt line for a PO line that is already fully received MUST return a 409 Conflict error with code `LINE_FULLY_RECEIVED`.
- Receiving with a batch number that already exists for the same product MUST reuse the existing batch (not create a duplicate).

#### 2.5.4 Get Goods Receipt

- The system MUST support retrieving a single goods receipt by ID, including all lines with product details and inspection status.

#### 2.5.5 List Goods Receipts

- The system MUST support paginated listing of goods receipts.
- The system MUST support filtering by: PO ID, PO number, receipt number, warehouse ID, date range.
- The system MUST default to sorting by received date descending.

### 2.6 Receiving Inspection

#### 2.6.1 Inspect Goods Receipt Line

- The system MUST support setting the inspection status of a goods receipt line to one of: `Accepted`, `Rejected`, `Quarantined`.
- The inspection MUST record an inspection note (optional), inspection date, and inspector user ID.
- Only lines with `Pending` inspection status can be inspected. Re-inspecting an already-inspected line MUST return a 409 Conflict error with code `LINE_ALREADY_INSPECTED`.
- When a line is `Rejected`, the received quantity MUST NOT count toward the PO line's received total. The system MUST recalculate PO status accordingly.
- When a line is `Quarantined`, the received quantity MUST be tracked separately (quarantined quantity) and MUST NOT count toward available stock. Quarantined stock SHOULD be flagged for the Quality service when available.

**Edge cases:**
- Inspecting a line on a goods receipt that belongs to a cancelled PO MUST return a 409 Conflict error.
- Accepting a previously quarantined or rejected line requires a new goods receipt (re-receive).

#### 2.6.2 Quarantine Resolution

- The system MUST support resolving a quarantined line to either `Accepted` or `Rejected`.
- Accepting a quarantined line MUST publish a `GoodsReceiptLineAcceptedEvent` to create the corresponding stock movement.
- Rejecting a quarantined line MUST create a supplier return record if the goods are to be returned.

### 2.7 Supplier Returns

#### 2.7.1 Create Supplier Return

- The system MUST support creating a supplier return with: supplier ID, reason, notes, and one or more return lines.
- Each return line MUST specify a product ID, warehouse ID, location ID (optional), quantity, and optional reference to the original goods receipt line.
- The system MUST auto-generate a unique return number following the format `SR-{YYYYMMDD}-{sequential number padded to 4 digits}` (e.g., `SR-20260406-0001`).
- The system MUST set the initial status to `Draft`.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

#### 2.7.2 Confirm Supplier Return

- When a supplier return is confirmed, the system MUST publish a `SupplierReturnCompletedEvent` via MassTransit.
- The event MUST contain: return ID, return number, supplier ID, and for each line: product ID, warehouse ID, location ID, quantity, batch ID (if applicable).
- The Inventory service (consumer) MUST create a `StockMovement` with reason code `SupplierReturn`, reference type `PurchaseOrder`, and negative quantity (outbound).
- The system MUST prevent confirming a return if any line would result in insufficient stock. This MUST return a 409 Conflict error with code `INSUFFICIENT_STOCK`.
- The system MUST record `ConfirmedAtUtc` and `ConfirmedByUserId` when confirmed.

**Edge cases:**
- Confirming an already-confirmed return MUST return a 409 Conflict error with code `RETURN_ALREADY_CONFIRMED`.
- Confirming a return for a product with zero stock at the specified location MUST return a 409 Conflict error with code `INSUFFICIENT_STOCK`.

#### 2.7.3 Cancel Supplier Return

- The system MUST support cancelling a `Draft` supplier return.
- Cancelling a confirmed return MUST return a 409 Conflict error with code `RETURN_ALREADY_CONFIRMED`.

#### 2.7.4 Get Supplier Return

- The system MUST support retrieving a single supplier return by ID, including all lines with product and supplier details.

#### 2.7.5 Search Supplier Returns

- The system MUST support paginated listing of supplier returns.
- The system MUST support filtering by: supplier ID, status, return number, date range.
- The system MUST default to sorting by created date descending.

### 2.8 Purchase Event History

- The system MUST maintain an immutable log of all procurement operations events.
- Events MUST include: event type, entity type, entity ID, user ID, timestamp, and a JSON payload with before/after state where applicable.
- Event types MUST include: `PurchaseOrderCreated`, `PurchaseOrderConfirmed`, `PurchaseOrderCancelled`, `PurchaseOrderClosed`, `GoodsReceiptCreated`, `GoodsReceiptCompleted`, `InspectionCompleted`, `SupplierReturnCreated`, `SupplierReturnConfirmed`, `SupplierReturnCancelled`.
- The system MUST support paginated querying of purchase events with filtering by event type, entity type, entity ID, user ID, and date range.

### 2.9 Inter-Service Event Contracts (MassTransit)

These are the first inter-service events in the system. Event contracts are defined in `Warehouse.ServiceModel/Events/`.

#### 2.9.1 GoodsReceiptCompletedEvent

- The system MUST publish this event when a goods receipt is completed with at least one accepted line.
- The event contract MUST include:
  - `GoodsReceiptId` (int)
  - `GoodsReceiptNumber` (string)
  - `PurchaseOrderId` (int)
  - `PurchaseOrderNumber` (string)
  - `WarehouseId` (int)
  - `LocationId` (int?)
  - `ReceivedByUserId` (int)
  - `ReceivedAtUtc` (DateTime)
  - `Lines` (collection of):
    - `GoodsReceiptLineId` (int)
    - `ProductId` (int)
    - `Quantity` (decimal)
    - `BatchNumber` (string?)
    - `ManufacturingDate` (DateOnly?)
    - `ExpiryDate` (DateOnly?)
- Event naming follows the convention `{Domain}.{Entity}.{PastTenseVerb}`: `Purchasing.GoodsReceipt.Completed`.

#### 2.9.2 SupplierReturnCompletedEvent

- The system MUST publish this event when a supplier return is confirmed.
- The event contract MUST include:
  - `SupplierReturnId` (int)
  - `SupplierReturnNumber` (string)
  - `SupplierId` (int)
  - `ConfirmedByUserId` (int)
  - `ConfirmedAtUtc` (DateTime)
  - `Lines` (collection of):
    - `SupplierReturnLineId` (int)
    - `ProductId` (int)
    - `WarehouseId` (int)
    - `LocationId` (int?)
    - `Quantity` (decimal)
    - `BatchId` (int?)
- Event naming: `Purchasing.SupplierReturn.Completed`.

#### 2.9.3 GoodsReceiptLineAcceptedEvent

- The system MUST publish this event when a quarantined goods receipt line is resolved to `Accepted`.
- The event contract MUST include:
  - `GoodsReceiptId` (int)
  - `GoodsReceiptLineId` (int)
  - `ProductId` (int)
  - `WarehouseId` (int)
  - `LocationId` (int?)
  - `Quantity` (decimal)
  - `BatchNumber` (string?)
  - `ManufacturingDate` (DateOnly?)
  - `ExpiryDate` (DateOnly?)
  - `AcceptedByUserId` (int)
  - `AcceptedAtUtc` (DateTime)
- Event naming: `Purchasing.GoodsReceiptLine.Accepted`.

---

## 3. Validation Rules

### 3.1 Supplier

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | Name | Required. 1-200 characters. | `INVALID_SUPPLIER_NAME` |
| V2 | Code | Optional on create (auto-generated if omitted). 1-20 characters. Alphanumeric + hyphens. | `INVALID_SUPPLIER_CODE` |
| V3 | TaxId | Optional. 1-50 characters when provided. | `INVALID_TAX_ID` |
| V4 | CategoryId | Optional. Must reference an existing category when provided. | `INVALID_CATEGORY` |
| V5 | Code | Must be unique across all suppliers (including soft-deleted). | `DUPLICATE_SUPPLIER_CODE` |
| V6 | TaxId | Must be unique across active suppliers when provided. | `DUPLICATE_TAX_ID` |
| V7 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V8 | PaymentTermDays | Optional. Must be >= 0 and <= 365 when provided. | `INVALID_PAYMENT_TERMS` |

### 3.2 Supplier Category

| # | Field | Rule | Error Code |
|---|---|---|---|
| V9 | Name | Required. 1-100 characters. | `INVALID_CATEGORY_NAME` |
| V10 | Description | Optional. Max 500 characters. | `INVALID_CATEGORY_DESCRIPTION` |
| V11 | Name | Must be unique across all supplier categories. | `DUPLICATE_CATEGORY_NAME` |

### 3.3 Supplier Address

| # | Field | Rule | Error Code |
|---|---|---|---|
| V12 | AddressType | Required. One of: `Billing`, `Shipping`, `Both`. | `INVALID_ADDRESS_TYPE` |
| V13 | StreetLine1 | Required. 1-200 characters. | `INVALID_STREET_LINE1` |
| V14 | StreetLine2 | Optional. Max 200 characters. | `INVALID_STREET_LINE2` |
| V15 | City | Required. 1-100 characters. | `INVALID_CITY` |
| V16 | StateProvince | Optional. Max 100 characters. | `INVALID_STATE_PROVINCE` |
| V17 | PostalCode | Required. 1-20 characters. | `INVALID_POSTAL_CODE` |
| V18 | CountryCode | Required. ISO 3166-1 alpha-2 (2-letter code). | `INVALID_COUNTRY_CODE` |

### 3.4 Supplier Phone

| # | Field | Rule | Error Code |
|---|---|---|---|
| V19 | PhoneType | Required. One of: `Mobile`, `Landline`, `Fax`. | `INVALID_PHONE_TYPE` |
| V20 | PhoneNumber | Required. 5-20 characters. Digits, spaces, hyphens, parentheses, and leading `+` allowed. | `INVALID_PHONE_NUMBER` |
| V21 | Extension | Optional. Max 10 characters. Digits only. | `INVALID_EXTENSION` |

### 3.5 Supplier Email

| # | Field | Rule | Error Code |
|---|---|---|---|
| V22 | EmailType | Required. One of: `General`, `Billing`, `Support`. | `INVALID_EMAIL_TYPE` |
| V23 | EmailAddress | Required. Valid email format (RFC 5322). Max 256 characters. | `INVALID_EMAIL_ADDRESS` |
| V24 | EmailAddress | Must be unique per supplier. | `DUPLICATE_SUPPLIER_EMAIL` |

### 3.6 Purchase Order

| # | Field | Rule | Error Code |
|---|---|---|---|
| V25 | SupplierId | Required. Must reference an existing, active supplier. | `INVALID_SUPPLIER` |
| V26 | DestinationWarehouseId | Required. Must reference an existing, active warehouse (cross-service: Inventory). | `INVALID_WAREHOUSE` |
| V27 | ExpectedDeliveryDate | Optional. Must be today or a future date when provided. | `INVALID_DELIVERY_DATE` |
| V28 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V29 | Lines | At least one line required on create. | `PO_MUST_HAVE_LINES` |

### 3.7 Purchase Order Line

| # | Field | Rule | Error Code |
|---|---|---|---|
| V30 | ProductId | Required. Must reference an existing, non-deleted product (cross-service: Inventory). | `INVALID_PRODUCT` |
| V31 | OrderedQuantity | Required. Must be > 0. | `INVALID_QUANTITY` |
| V32 | UnitPrice | Required. Must be >= 0. | `INVALID_UNIT_PRICE` |
| V33 | ProductId | Must be unique within the same PO. | `DUPLICATE_PO_LINE` |
| V34 | Notes | Optional. Max 500 characters. | `INVALID_LINE_NOTES` |

### 3.8 Goods Receipt

| # | Field | Rule | Error Code |
|---|---|---|---|
| V35 | PurchaseOrderId | Required. Must reference a PO in `Confirmed` or `PartiallyReceived` status. | `PO_NOT_RECEIVABLE` |
| V36 | WarehouseId | Required. Must reference an existing, active warehouse. | `INVALID_WAREHOUSE` |
| V37 | LocationId | Optional. Must reference an existing location within the specified warehouse when provided. | `INVALID_LOCATION` |

### 3.9 Goods Receipt Line

| # | Field | Rule | Error Code |
|---|---|---|---|
| V38 | PurchaseOrderLineId | Required. Must reference an existing PO line belonging to the receipt's PO. | `INVALID_PO_LINE` |
| V39 | ReceivedQuantity | Required. Must be > 0. | `INVALID_QUANTITY` |
| V40 | ReceivedQuantity | Must not exceed remaining quantity on the PO line (within tolerance). | `OVER_RECEIPT` |
| V41 | BatchNumber | Optional. Max 50 characters. | `INVALID_BATCH_NUMBER` |
| V42 | ManufacturingDate | Optional. Must not be in the future when provided. | `INVALID_MANUFACTURING_DATE` |
| V43 | ExpiryDate | Optional. Must be after manufacturing date when both are provided. | `INVALID_EXPIRY_DATE` |

### 3.10 Receiving Inspection

| # | Field | Rule | Error Code |
|---|---|---|---|
| V44 | InspectionStatus | Required. One of: `Accepted`, `Rejected`, `Quarantined`. | `INVALID_INSPECTION_STATUS` |
| V45 | InspectionNote | Optional. Max 2000 characters. | `INVALID_INSPECTION_NOTE` |

### 3.11 Supplier Return

| # | Field | Rule | Error Code |
|---|---|---|---|
| V46 | SupplierId | Required. Must reference an existing, active supplier. | `INVALID_SUPPLIER` |
| V47 | Reason | Required. 1-500 characters. | `INVALID_RETURN_REASON` |
| V48 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V49 | Lines | At least one line required on create. | `RETURN_MUST_HAVE_LINES` |

### 3.12 Supplier Return Line

| # | Field | Rule | Error Code |
|---|---|---|---|
| V50 | ProductId | Required. Must reference an existing product. | `INVALID_PRODUCT` |
| V51 | WarehouseId | Required. Must reference an existing warehouse. | `INVALID_WAREHOUSE` |
| V52 | LocationId | Optional. Must reference an existing location within the warehouse when provided. | `INVALID_LOCATION` |
| V53 | Quantity | Required. Must be > 0. | `INVALID_QUANTITY` |
| V54 | GoodsReceiptLineId | Optional. Must reference an existing accepted goods receipt line when provided. | `INVALID_RECEIPT_LINE` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| **Supplier errors** | | | | |
| E1 | Supplier not found (or soft-deleted) | 404 | `SUPPLIER_NOT_FOUND` | Supplier not found. |
| E2 | Duplicate supplier code | 409 | `DUPLICATE_SUPPLIER_CODE` | A supplier with this code already exists. |
| E3 | Duplicate tax ID (among active suppliers) | 409 | `DUPLICATE_TAX_ID` | An active supplier with this tax ID already exists. |
| E4 | Invalid category reference | 400 | `INVALID_CATEGORY` | The specified supplier category does not exist. |
| E5 | Category not found | 404 | `CATEGORY_NOT_FOUND` | Supplier category not found. |
| E6 | Delete category with suppliers | 409 | `CATEGORY_IN_USE` | Cannot delete category -- it is assigned to {count} supplier(s). |
| E7 | Duplicate category name | 409 | `DUPLICATE_CATEGORY_NAME` | A category with this name already exists. |
| E8 | Deactivate supplier with open POs | 409 | `SUPPLIER_HAS_OPEN_POS` | Cannot deactivate supplier -- {count} purchase order(s) are still open. |
| E9 | Supplier inactive | 409 | `SUPPLIER_INACTIVE` | The supplier is inactive and cannot be used for new purchase orders. |
| **Contact errors** | | | | |
| E10 | Address not found | 404 | `ADDRESS_NOT_FOUND` | Supplier address not found. |
| E11 | Phone not found | 404 | `PHONE_NOT_FOUND` | Supplier phone not found. |
| E12 | Email not found | 404 | `EMAIL_NOT_FOUND` | Supplier email not found. |
| E13 | Duplicate email per supplier | 409 | `DUPLICATE_SUPPLIER_EMAIL` | This supplier already has this email address. |
| **Purchase Order errors** | | | | |
| E14 | PO not found | 404 | `PO_NOT_FOUND` | Purchase order not found. |
| E15 | PO line not found | 404 | `PO_LINE_NOT_FOUND` | Purchase order line not found. |
| E16 | PO not editable (not Draft) | 409 | `PO_NOT_EDITABLE` | Purchase order can only be edited in Draft status. |
| E17 | Invalid PO status transition | 409 | `INVALID_PO_STATUS_TRANSITION` | Cannot transition purchase order from {from} to {to}. |
| E18 | PO must have lines | 409 | `PO_MUST_HAVE_LINES` | Purchase order must have at least one line. |
| E19 | Duplicate PO line (same product) | 409 | `DUPLICATE_PO_LINE` | This product is already on the purchase order. |
| E20 | PO has receipts (cannot cancel) | 409 | `PO_HAS_RECEIPTS` | Cannot cancel purchase order -- goods have been received. |
| E21 | PO already closed | 409 | `PO_ALREADY_CLOSED` | Purchase order is already closed. |
| E22 | Invalid product reference | 400 | `INVALID_PRODUCT` | The specified product does not exist or is inactive. |
| E23 | Invalid warehouse reference | 400 | `INVALID_WAREHOUSE` | The specified warehouse does not exist or is inactive. |
| **Goods Receipt errors** | | | | |
| E24 | Goods receipt not found | 404 | `RECEIPT_NOT_FOUND` | Goods receipt not found. |
| E25 | PO not receivable | 409 | `PO_NOT_RECEIVABLE` | Purchase order is not in a receivable status. |
| E26 | Over-receipt | 409 | `OVER_RECEIPT` | Received quantity exceeds remaining quantity on the PO line. |
| E27 | Line fully received | 409 | `LINE_FULLY_RECEIVED` | This PO line has already been fully received. |
| E28 | Invalid PO line reference | 400 | `INVALID_PO_LINE` | The specified PO line does not belong to this purchase order. |
| E29 | Invalid location reference | 400 | `INVALID_LOCATION` | The specified location does not exist or does not belong to the warehouse. |
| **Inspection errors** | | | | |
| E30 | Line already inspected | 409 | `LINE_ALREADY_INSPECTED` | This goods receipt line has already been inspected. |
| E31 | Goods receipt line not found | 404 | `RECEIPT_LINE_NOT_FOUND` | Goods receipt line not found. |
| **Supplier Return errors** | | | | |
| E32 | Supplier return not found | 404 | `RETURN_NOT_FOUND` | Supplier return not found. |
| E33 | Return already confirmed | 409 | `RETURN_ALREADY_CONFIRMED` | Supplier return has already been confirmed. |
| E34 | Insufficient stock for return | 409 | `INSUFFICIENT_STOCK` | Insufficient stock at the specified location for this return. |
| E35 | Return must have lines | 400 | `RETURN_MUST_HAVE_LINES` | Supplier return must have at least one line. |
| **Common errors** | | | | |
| E36 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more fields are invalid. (ProblemDetails with field errors) |
| E37 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E38 | Unauthenticated | 401 | `UNAUTHORIZED` | Authentication is required. |

All error responses MUST use ProblemDetails (RFC 7807) format:

```json
{
  "type": "https://warehouse.local/errors/{error-code}",
  "title": "Short error title",
  "status": 400,
  "detail": "Human-readable description.",
  "instance": "/api/v1/purchase-orders/{id}",
  "errors": {}
}
```

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| **Suppliers** | | | | |
| POST | `/api/v1/suppliers` | Create supplier | Yes | `suppliers:create` |
| GET | `/api/v1/suppliers` | List/search suppliers (paginated) | Yes | `suppliers:read` |
| GET | `/api/v1/suppliers/{id}` | Get supplier by ID (with contacts, category) | Yes | `suppliers:read` |
| PUT | `/api/v1/suppliers/{id}` | Update supplier | Yes | `suppliers:update` |
| DELETE | `/api/v1/suppliers/{id}` | Soft-delete (deactivate) supplier | Yes | `suppliers:delete` |
| POST | `/api/v1/suppliers/{id}/reactivate` | Reactivate soft-deleted supplier | Yes | `suppliers:update` |
| **Supplier Categories** | | | | |
| POST | `/api/v1/supplier-categories` | Create category | Yes | `suppliers:create` |
| GET | `/api/v1/supplier-categories` | List categories | Yes | `suppliers:read` |
| GET | `/api/v1/supplier-categories/{id}` | Get category by ID | Yes | `suppliers:read` |
| PUT | `/api/v1/supplier-categories/{id}` | Update category | Yes | `suppliers:update` |
| DELETE | `/api/v1/supplier-categories/{id}` | Delete category | Yes | `suppliers:delete` |
| **Supplier Addresses** | | | | |
| POST | `/api/v1/suppliers/{supplierId}/addresses` | Create address | Yes | `suppliers:update` |
| GET | `/api/v1/suppliers/{supplierId}/addresses` | List addresses | Yes | `suppliers:read` |
| PUT | `/api/v1/suppliers/{supplierId}/addresses/{addressId}` | Update address | Yes | `suppliers:update` |
| DELETE | `/api/v1/suppliers/{supplierId}/addresses/{addressId}` | Delete address | Yes | `suppliers:update` |
| **Supplier Phones** | | | | |
| POST | `/api/v1/suppliers/{supplierId}/phones` | Create phone | Yes | `suppliers:update` |
| GET | `/api/v1/suppliers/{supplierId}/phones` | List phones | Yes | `suppliers:read` |
| PUT | `/api/v1/suppliers/{supplierId}/phones/{phoneId}` | Update phone | Yes | `suppliers:update` |
| DELETE | `/api/v1/suppliers/{supplierId}/phones/{phoneId}` | Delete phone | Yes | `suppliers:update` |
| **Supplier Emails** | | | | |
| POST | `/api/v1/suppliers/{supplierId}/emails` | Create email | Yes | `suppliers:update` |
| GET | `/api/v1/suppliers/{supplierId}/emails` | List emails | Yes | `suppliers:read` |
| PUT | `/api/v1/suppliers/{supplierId}/emails/{emailId}` | Update email | Yes | `suppliers:update` |
| DELETE | `/api/v1/suppliers/{supplierId}/emails/{emailId}` | Delete email | Yes | `suppliers:update` |
| **Purchase Orders** | | | | |
| POST | `/api/v1/purchase-orders` | Create PO (with lines) | Yes | `purchase-orders:create` |
| GET | `/api/v1/purchase-orders` | List/search POs (paginated) | Yes | `purchase-orders:read` |
| GET | `/api/v1/purchase-orders/{id}` | Get PO by ID (with lines, progress) | Yes | `purchase-orders:read` |
| PUT | `/api/v1/purchase-orders/{id}` | Update PO header | Yes | `purchase-orders:update` |
| POST | `/api/v1/purchase-orders/{id}/confirm` | Confirm PO (Draft -> Confirmed) | Yes | `purchase-orders:update` |
| POST | `/api/v1/purchase-orders/{id}/cancel` | Cancel PO | Yes | `purchase-orders:update` |
| POST | `/api/v1/purchase-orders/{id}/close` | Close PO | Yes | `purchase-orders:update` |
| **Purchase Order Lines** | | | | |
| POST | `/api/v1/purchase-orders/{poId}/lines` | Add line to PO | Yes | `purchase-orders:update` |
| PUT | `/api/v1/purchase-orders/{poId}/lines/{lineId}` | Update PO line | Yes | `purchase-orders:update` |
| DELETE | `/api/v1/purchase-orders/{poId}/lines/{lineId}` | Remove PO line | Yes | `purchase-orders:update` |
| **Goods Receiving** | | | | |
| POST | `/api/v1/goods-receipts` | Create goods receipt (with lines) | Yes | `goods-receipts:create` |
| GET | `/api/v1/goods-receipts` | List goods receipts (paginated) | Yes | `goods-receipts:read` |
| GET | `/api/v1/goods-receipts/{id}` | Get goods receipt by ID (with lines) | Yes | `goods-receipts:read` |
| POST | `/api/v1/goods-receipts/{id}/complete` | Complete goods receipt | Yes | `goods-receipts:update` |
| **Receiving Inspection** | | | | |
| POST | `/api/v1/goods-receipts/{receiptId}/lines/{lineId}/inspect` | Inspect a receipt line | Yes | `goods-receipts:update` |
| POST | `/api/v1/goods-receipts/{receiptId}/lines/{lineId}/resolve` | Resolve quarantined line | Yes | `goods-receipts:update` |
| **Supplier Returns** | | | | |
| POST | `/api/v1/supplier-returns` | Create supplier return (with lines) | Yes | `supplier-returns:create` |
| GET | `/api/v1/supplier-returns` | List supplier returns (paginated) | Yes | `supplier-returns:read` |
| GET | `/api/v1/supplier-returns/{id}` | Get supplier return by ID (with lines) | Yes | `supplier-returns:read` |
| POST | `/api/v1/supplier-returns/{id}/confirm` | Confirm supplier return | Yes | `supplier-returns:update` |
| POST | `/api/v1/supplier-returns/{id}/cancel` | Cancel supplier return | Yes | `supplier-returns:update` |
| **Purchase Events** | | | | |
| GET | `/api/v1/purchase-events` | List purchase events (paginated) | Yes | `purchase-events:read` |
| **Health** | | | | |
| GET | `/health` | Liveness check | No | -- |
| GET | `/health/ready` | Readiness check (DB + RabbitMQ) | No | -- |

---

## 6. Database Schema

**Schema name:** `purchasing`

### Tables

**purchasing.Suppliers**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Code | NVARCHAR(20) | NOT NULL, UNIQUE |
| Name | NVARCHAR(200) | NOT NULL |
| TaxId | NVARCHAR(50) | NULL |
| CategoryId | INT | NULL, FK -> purchasing.SupplierCategories(Id) |
| PaymentTermDays | INT | NULL |
| Notes | NVARCHAR(2000) | NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| IsDeleted | BIT | NOT NULL, DEFAULT 0 |
| DeletedAtUtc | DATETIME2(7) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users -- no EF navigation) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (cross-schema ref to auth.Users -- no EF navigation) |

**purchasing.SupplierCategories**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Name | NVARCHAR(100) | NOT NULL, UNIQUE |
| Description | NVARCHAR(500) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**purchasing.SupplierAddresses**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| SupplierId | INT | NOT NULL, FK -> purchasing.Suppliers(Id) |
| AddressType | NVARCHAR(20) | NOT NULL |
| StreetLine1 | NVARCHAR(200) | NOT NULL |
| StreetLine2 | NVARCHAR(200) | NULL |
| City | NVARCHAR(100) | NOT NULL |
| StateProvince | NVARCHAR(100) | NULL |
| PostalCode | NVARCHAR(20) | NOT NULL |
| CountryCode | NVARCHAR(2) | NOT NULL |
| IsDefault | BIT | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**purchasing.SupplierPhones**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| SupplierId | INT | NOT NULL, FK -> purchasing.Suppliers(Id) |
| PhoneType | NVARCHAR(20) | NOT NULL |
| PhoneNumber | NVARCHAR(20) | NOT NULL |
| Extension | NVARCHAR(10) | NULL |
| IsPrimary | BIT | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**purchasing.SupplierEmails**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| SupplierId | INT | NOT NULL, FK -> purchasing.Suppliers(Id) |
| EmailType | NVARCHAR(20) | NOT NULL |
| EmailAddress | NVARCHAR(256) | NOT NULL |
| IsPrimary | BIT | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**purchasing.PurchaseOrders**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| OrderNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| SupplierId | INT | NOT NULL, FK -> purchasing.Suppliers(Id) |
| Status | NVARCHAR(30) | NOT NULL, DEFAULT 'Draft' |
| DestinationWarehouseId | INT | NOT NULL (cross-schema ref to inventory.Warehouses -- no EF navigation) |
| ExpectedDeliveryDate | DATE | NULL |
| Notes | NVARCHAR(2000) | NULL |
| TotalAmount | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (cross-schema ref to auth.Users) |
| ConfirmedAtUtc | DATETIME2(7) | NULL |
| ConfirmedByUserId | INT | NULL (cross-schema ref to auth.Users) |
| ClosedAtUtc | DATETIME2(7) | NULL |
| ClosedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**purchasing.PurchaseOrderLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| PurchaseOrderId | INT | NOT NULL, FK -> purchasing.PurchaseOrders(Id) |
| ProductId | INT | NOT NULL (cross-schema ref to inventory.Products -- no EF navigation) |
| OrderedQuantity | DECIMAL(18,4) | NOT NULL |
| UnitPrice | DECIMAL(18,4) | NOT NULL |
| LineTotal | DECIMAL(18,4) | NOT NULL (computed: OrderedQuantity * UnitPrice) |
| ReceivedQuantity | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| Notes | NVARCHAR(500) | NULL |

**purchasing.GoodsReceipts**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ReceiptNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| PurchaseOrderId | INT | NOT NULL, FK -> purchasing.PurchaseOrders(Id) |
| WarehouseId | INT | NOT NULL (cross-schema ref to inventory.Warehouses) |
| LocationId | INT | NULL (cross-schema ref to inventory.StorageLocations) |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Open' |
| Notes | NVARCHAR(2000) | NULL |
| ReceivedAtUtc | DATETIME2(7) | NOT NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| CompletedAtUtc | DATETIME2(7) | NULL |

**purchasing.GoodsReceiptLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| GoodsReceiptId | INT | NOT NULL, FK -> purchasing.GoodsReceipts(Id) |
| PurchaseOrderLineId | INT | NOT NULL, FK -> purchasing.PurchaseOrderLines(Id) |
| ReceivedQuantity | DECIMAL(18,4) | NOT NULL |
| BatchNumber | NVARCHAR(50) | NULL |
| ManufacturingDate | DATE | NULL |
| ExpiryDate | DATE | NULL |
| InspectionStatus | NVARCHAR(20) | NOT NULL, DEFAULT 'Pending' |
| InspectionNote | NVARCHAR(2000) | NULL |
| InspectedAtUtc | DATETIME2(7) | NULL |
| InspectedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**purchasing.SupplierReturns**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ReturnNumber | NVARCHAR(20) | NOT NULL, UNIQUE |
| SupplierId | INT | NOT NULL, FK -> purchasing.Suppliers(Id) |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Draft' |
| Reason | NVARCHAR(500) | NOT NULL |
| Notes | NVARCHAR(2000) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ConfirmedAtUtc | DATETIME2(7) | NULL |
| ConfirmedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**purchasing.SupplierReturnLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| SupplierReturnId | INT | NOT NULL, FK -> purchasing.SupplierReturns(Id) |
| ProductId | INT | NOT NULL (cross-schema ref to inventory.Products) |
| WarehouseId | INT | NOT NULL (cross-schema ref to inventory.Warehouses) |
| LocationId | INT | NULL (cross-schema ref to inventory.StorageLocations) |
| Quantity | DECIMAL(18,4) | NOT NULL |
| BatchId | INT | NULL (cross-schema ref to inventory.Batches) |
| GoodsReceiptLineId | INT | NULL, FK -> purchasing.GoodsReceiptLines(Id) |
| Notes | NVARCHAR(500) | NULL |

**purchasing.PurchaseEvents**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| EventType | NVARCHAR(50) | NOT NULL |
| EntityType | NVARCHAR(50) | NOT NULL |
| EntityId | INT | NOT NULL |
| UserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| OccurredAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| Payload | NVARCHAR(MAX) | NULL |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_Suppliers_Code | Suppliers | Code | Unique |
| IX_Suppliers_TaxId | Suppliers | TaxId | Non-unique (filtered: IsDeleted = 0 AND TaxId IS NOT NULL) |
| IX_Suppliers_CategoryId | Suppliers | CategoryId | Non-unique |
| IX_Suppliers_IsDeleted | Suppliers | IsDeleted | Non-unique (filtered: IsDeleted = 0) |
| IX_Suppliers_Name | Suppliers | Name | Non-unique |
| IX_SupplierCategories_Name | SupplierCategories | Name | Unique |
| IX_SupplierAddresses_SupplierId | SupplierAddresses | SupplierId | Non-unique |
| IX_SupplierPhones_SupplierId | SupplierPhones | SupplierId | Non-unique |
| IX_SupplierEmails_SupplierId | SupplierEmails | SupplierId | Non-unique |
| IX_SupplierEmails_SupplierId_EmailAddress | SupplierEmails | SupplierId, EmailAddress | Unique |
| IX_PurchaseOrders_OrderNumber | PurchaseOrders | OrderNumber | Unique |
| IX_PurchaseOrders_SupplierId | PurchaseOrders | SupplierId | Non-unique |
| IX_PurchaseOrders_Status | PurchaseOrders | Status | Non-unique |
| IX_PurchaseOrders_CreatedAtUtc | PurchaseOrders | CreatedAtUtc | Non-unique |
| IX_PurchaseOrders_DestinationWarehouseId | PurchaseOrders | DestinationWarehouseId | Non-unique |
| IX_PurchaseOrderLines_PurchaseOrderId | PurchaseOrderLines | PurchaseOrderId | Non-unique |
| IX_PurchaseOrderLines_ProductId | PurchaseOrderLines | ProductId | Non-unique |
| IX_PurchaseOrderLines_POId_ProductId | PurchaseOrderLines | PurchaseOrderId, ProductId | Unique |
| IX_GoodsReceipts_ReceiptNumber | GoodsReceipts | ReceiptNumber | Unique |
| IX_GoodsReceipts_PurchaseOrderId | GoodsReceipts | PurchaseOrderId | Non-unique |
| IX_GoodsReceipts_WarehouseId | GoodsReceipts | WarehouseId | Non-unique |
| IX_GoodsReceipts_ReceivedAtUtc | GoodsReceipts | ReceivedAtUtc | Non-unique |
| IX_GoodsReceiptLines_GoodsReceiptId | GoodsReceiptLines | GoodsReceiptId | Non-unique |
| IX_GoodsReceiptLines_PurchaseOrderLineId | GoodsReceiptLines | PurchaseOrderLineId | Non-unique |
| IX_SupplierReturns_ReturnNumber | SupplierReturns | ReturnNumber | Unique |
| IX_SupplierReturns_SupplierId | SupplierReturns | SupplierId | Non-unique |
| IX_SupplierReturns_Status | SupplierReturns | Status | Non-unique |
| IX_SupplierReturnLines_SupplierReturnId | SupplierReturnLines | SupplierReturnId | Non-unique |
| IX_PurchaseEvents_EventType | PurchaseEvents | EventType | Non-unique |
| IX_PurchaseEvents_EntityType_EntityId | PurchaseEvents | EntityType, EntityId | Non-unique |
| IX_PurchaseEvents_OccurredAtUtc | PurchaseEvents | OccurredAtUtc | Non-unique |

---

## 7. Versioning Notes

- **v1 -- Initial specification (2026-04-06)**
  - Supplier CRUD with soft delete, reactivation, categories, and contact information (addresses, phones, emails)
  - Purchase order lifecycle with status machine (Draft -> Confirmed -> PartiallyReceived -> Received -> Closed)
  - Purchase order lines referencing cross-service product catalog
  - Goods receiving against PO lines with batch auto-creation
  - Receiving inspection (accept/reject/quarantine) per goods receipt line
  - Quarantine resolution workflow
  - Supplier returns with stock movement generation
  - Immutable purchase event history
  - MassTransit event contracts: GoodsReceiptCompletedEvent, SupplierReturnCompletedEvent, GoodsReceiptLineAcceptedEvent
  - ProblemDetails error responses
  - Full validation rule set
  - Database schema on `purchasing` schema

---

## 8. Test Plan

### Unit Tests -- SupplierServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedSupplier` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateTaxId_ReturnsConflictError` [Unit]
- `CreateAsync_InvalidCategoryId_ReturnsValidationError` [Unit]
- `CreateAsync_NoCodeProvided_GeneratesCode` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedSupplier` [Unit]
- `UpdateAsync_SoftDeletedSupplier_ReturnsNotFound` [Unit]
- `UpdateAsync_DuplicateTaxId_ReturnsConflictError` [Unit]
- `GetByIdAsync_ExistingSupplier_ReturnsSupplierWithDetails` [Unit]
- `GetByIdAsync_SoftDeletedSupplier_ReturnsNotFound` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `SearchAsync_DefaultSort_SortsByNameAscending` [Unit]
- `DeactivateAsync_ActiveSupplier_SetsIsDeletedAndDeletedAt` [Unit]
- `DeactivateAsync_SupplierWithOpenPOs_ReturnsConflict` [Unit]
- `DeactivateAsync_AlreadyDeleted_ReturnsNotFound` [Unit]
- `ReactivateAsync_SoftDeletedSupplier_ClearsDeletedFlags` [Unit]
- `ReactivateAsync_ConflictingCode_ReturnsConflictError` [Unit]
- `ReactivateAsync_ConflictingTaxId_ReturnsConflictError` [Unit]

### Unit Tests -- SupplierCategoryServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedCategory` [Unit]
- `CreateAsync_DuplicateName_ReturnsConflictError` [Unit]
- `UpdateAsync_DuplicateName_ReturnsConflictError` [Unit]
- `DeleteAsync_CategoryWithSuppliers_ReturnsConflict` [Unit]
- `DeleteAsync_UnusedCategory_DeletesSuccessfully` [Unit]

### Unit Tests -- SupplierContactServiceTests

- `CreateAddress_ValidRequest_ReturnsCreatedAddress` [Unit]
- `CreateAddress_FirstOfType_SetsDefaultTrue` [Unit]
- `DeleteAddress_DefaultAddress_PromotesNext` [Unit]
- `CreatePhone_FirstPhone_SetsPrimaryTrue` [Unit]
- `UpdatePhone_SetPrimary_UnsetsOtherPrimary` [Unit]
- `DeletePhone_PrimaryPhone_PromotesNext` [Unit]
- `CreateEmail_ValidRequest_ReturnsCreatedEmail` [Unit]
- `CreateEmail_DuplicateForSupplier_ReturnsConflict` [Unit]
- `CreateEmail_FirstEmail_SetsPrimaryTrue` [Unit]
- `UpdateEmail_DuplicateForSupplier_ReturnsConflict` [Unit]
- `DeleteEmail_PrimaryEmail_PromotesNext` [Unit]

### Unit Tests -- PurchaseOrderServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedPO` [Unit]
- `CreateAsync_NonExistentSupplier_ReturnsNotFound` [Unit]
- `CreateAsync_InactiveSupplier_ReturnsConflict` [Unit]
- `CreateAsync_NoLines_ReturnsValidationError` [Unit]
- `CreateAsync_InvalidWarehouseId_ReturnsValidationError` [Unit]
- `CreateAsync_GeneratesPONumber` [Unit]
- `UpdateHeaderAsync_DraftPO_ReturnsUpdatedPO` [Unit]
- `UpdateHeaderAsync_ConfirmedPO_ReturnsConflictError` [Unit]
- `AddLineAsync_DraftPO_ReturnsCreatedLine` [Unit]
- `AddLineAsync_ConfirmedPO_ReturnsConflictError` [Unit]
- `AddLineAsync_DuplicateProduct_ReturnsConflictError` [Unit]
- `UpdateLineAsync_DraftPO_ReturnsUpdatedLine` [Unit]
- `RemoveLineAsync_DraftPO_RemovesSuccessfully` [Unit]
- `RemoveLineAsync_LastLine_ReturnsConflictError` [Unit]
- `GetByIdAsync_ExistingPO_ReturnsPOWithLinesAndProgress` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `SearchAsync_DefaultSort_SortsByCreatedDateDescending` [Unit]

### Unit Tests -- PurchaseOrderStatusTests

- `ConfirmAsync_DraftPO_TransitionsToConfirmed` [Unit]
- `ConfirmAsync_NonDraftPO_ReturnsConflictError` [Unit]
- `ConfirmAsync_POWithNoLines_ReturnsConflictError` [Unit]
- `CancelAsync_DraftPO_TransitionsToCancelled` [Unit]
- `CancelAsync_ConfirmedPOWithNoReceipts_TransitionsToCancelled` [Unit]
- `CancelAsync_PartiallyReceivedPO_ReturnsConflictError` [Unit]
- `CancelAsync_POWithReceipts_ReturnsConflictError` [Unit]
- `CloseAsync_PartiallyReceivedPO_TransitionsToClosed` [Unit]
- `CloseAsync_ReceivedPO_TransitionsToClosed` [Unit]
- `CloseAsync_DraftPO_ReturnsConflictError` [Unit]
- `CloseAsync_AlreadyClosedPO_ReturnsConflictError` [Unit]

### Unit Tests -- GoodsReceiptServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedReceipt` [Unit]
- `CreateAsync_CancelledPO_ReturnsConflictError` [Unit]
- `CreateAsync_DraftPO_ReturnsConflictError` [Unit]
- `CreateAsync_InvalidWarehouseId_ReturnsValidationError` [Unit]
- `CreateAsync_GeneratesReceiptNumber` [Unit]
- `AddLineAsync_ValidRequest_ReturnsCreatedLine` [Unit]
- `AddLineAsync_OverReceipt_ReturnsConflictError` [Unit]
- `AddLineAsync_FullyReceivedLine_ReturnsConflictError` [Unit]
- `AddLineAsync_InvalidPOLineId_ReturnsValidationError` [Unit]
- `CompleteAsync_AcceptedLines_PublishesGoodsReceiptCompletedEvent` [Unit]
- `CompleteAsync_UpdatesPOStatusToPartiallyReceived` [Unit]
- `CompleteAsync_AllLinesReceived_UpdatesPOStatusToReceived` [Unit]
- `GetByIdAsync_ExistingReceipt_ReturnsReceiptWithLines` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]

### Unit Tests -- ReceivingInspectionServiceTests

- `InspectAsync_PendingLine_SetsStatusToAccepted` [Unit]
- `InspectAsync_PendingLine_SetsStatusToRejected` [Unit]
- `InspectAsync_PendingLine_SetsStatusToQuarantined` [Unit]
- `InspectAsync_AlreadyInspected_ReturnsConflictError` [Unit]
- `InspectAsync_RejectedLine_DoesNotCountTowardReceivedTotal` [Unit]
- `ResolveQuarantineAsync_AcceptedResolution_PublishesEvent` [Unit]
- `ResolveQuarantineAsync_RejectedResolution_DoesNotPublishEvent` [Unit]
- `ResolveQuarantineAsync_NonQuarantinedLine_ReturnsConflictError` [Unit]

### Unit Tests -- SupplierReturnServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedReturn` [Unit]
- `CreateAsync_NoLines_ReturnsValidationError` [Unit]
- `CreateAsync_InactiveSupplier_ReturnsConflict` [Unit]
- `CreateAsync_GeneratesReturnNumber` [Unit]
- `ConfirmAsync_DraftReturn_PublishesSupplierReturnCompletedEvent` [Unit]
- `ConfirmAsync_InsufficientStock_ReturnsConflictError` [Unit]
- `ConfirmAsync_AlreadyConfirmed_ReturnsConflictError` [Unit]
- `CancelAsync_DraftReturn_TransitionsToCancelled` [Unit]
- `CancelAsync_ConfirmedReturn_ReturnsConflictError` [Unit]
- `GetByIdAsync_ExistingReturn_ReturnsReturnWithLines` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]

### Unit Tests -- PurchaseEventServiceTests

- `RecordEventAsync_ValidEvent_PersistsEvent` [Unit]
- `SearchAsync_ByEventType_ReturnsMatchingEvents` [Unit]
- `SearchAsync_ByEntityTypeAndId_ReturnsMatchingEvents` [Unit]
- `SearchAsync_ByDateRange_ReturnsMatchingEvents` [Unit]

### Unit Tests -- Validation

- `CreateSupplierRequestValidator_MissingName_Fails` [Unit]
- `CreateSupplierRequestValidator_NameTooLong_Fails` [Unit]
- `CreateSupplierRequestValidator_CodeTooLong_Fails` [Unit]
- `CreateSupplierRequestValidator_InvalidCodeCharacters_Fails` [Unit]
- `CreateSupplierRequestValidator_PaymentTermsNegative_Fails` [Unit]
- `CreateSupplierRequestValidator_PaymentTermsTooHigh_Fails` [Unit]
- `CreateCategoryRequestValidator_MissingName_Fails` [Unit]
- `CreateCategoryRequestValidator_NameTooLong_Fails` [Unit]
- `CreateAddressRequestValidator_MissingRequiredFields_Fails` [Unit]
- `CreateAddressRequestValidator_InvalidAddressType_Fails` [Unit]
- `CreateAddressRequestValidator_InvalidCountryCode_Fails` [Unit]
- `CreatePhoneRequestValidator_InvalidPhoneNumber_Fails` [Unit]
- `CreatePhoneRequestValidator_InvalidPhoneType_Fails` [Unit]
- `CreateEmailRequestValidator_InvalidEmailFormat_Fails` [Unit]
- `CreateEmailRequestValidator_InvalidEmailType_Fails` [Unit]
- `CreatePurchaseOrderRequestValidator_MissingSupplierId_Fails` [Unit]
- `CreatePurchaseOrderRequestValidator_EmptyLines_Fails` [Unit]
- `CreatePurchaseOrderRequestValidator_PastDeliveryDate_Fails` [Unit]
- `CreatePOLineRequestValidator_ZeroQuantity_Fails` [Unit]
- `CreatePOLineRequestValidator_NegativeUnitPrice_Fails` [Unit]
- `CreateGoodsReceiptRequestValidator_MissingPOId_Fails` [Unit]
- `CreateGoodsReceiptLineRequestValidator_ZeroQuantity_Fails` [Unit]
- `CreateGoodsReceiptLineRequestValidator_FutureManufacturingDate_Fails` [Unit]
- `CreateGoodsReceiptLineRequestValidator_ExpiryBeforeManufacturing_Fails` [Unit]
- `InspectLineRequestValidator_InvalidStatus_Fails` [Unit]
- `CreateSupplierReturnRequestValidator_EmptyReason_Fails` [Unit]
- `CreateSupplierReturnRequestValidator_EmptyLines_Fails` [Unit]
- `CreateReturnLineRequestValidator_ZeroQuantity_Fails` [Unit]

### Integration Tests -- SuppliersControllerTests

- `CreateSupplier_ValidPayload_Returns201` [Integration]
- `CreateSupplier_DuplicateCode_Returns409` [Integration]
- `CreateSupplier_DuplicateTaxId_Returns409` [Integration]
- `CreateSupplier_InvalidPayload_Returns400ProblemDetails` [Integration]
- `CreateSupplier_Unauthenticated_Returns401` [Integration]
- `CreateSupplier_InsufficientPermissions_Returns403` [Integration]
- `GetSupplier_ExistingId_Returns200WithDetails` [Integration]
- `GetSupplier_SoftDeletedId_Returns404` [Integration]
- `UpdateSupplier_ValidPayload_Returns200` [Integration]
- `DeleteSupplier_ActiveSupplier_Returns204` [Integration]
- `SearchSuppliers_WithNameFilter_ReturnsMatchingResults` [Integration]
- `SearchSuppliers_Pagination_ReturnsCorrectPage` [Integration]
- `ReactivateSupplier_SoftDeleted_Returns200` [Integration]

### Integration Tests -- SupplierCategoriesControllerTests

- `CreateCategory_ValidPayload_Returns201` [Integration]
- `CreateCategory_DuplicateName_Returns409` [Integration]
- `ListCategories_Returns200` [Integration]
- `DeleteCategory_WithSuppliers_Returns409` [Integration]
- `DeleteCategory_Unused_Returns204` [Integration]

### Integration Tests -- SupplierContactsControllerTests

- `CreateAddress_ValidPayload_Returns201` [Integration]
- `ListAddresses_ReturnsAllForSupplier` [Integration]
- `UpdateAddress_ValidPayload_Returns200` [Integration]
- `DeleteAddress_Returns204` [Integration]
- `CreatePhone_ValidPayload_Returns201` [Integration]
- `ListPhones_ReturnsAllForSupplier` [Integration]
- `DeletePhone_Returns204` [Integration]
- `CreateEmail_ValidPayload_Returns201` [Integration]
- `CreateEmail_DuplicateForSupplier_Returns409` [Integration]
- `ListEmails_ReturnsAllForSupplier` [Integration]
- `DeleteEmail_Returns204` [Integration]

### Integration Tests -- PurchaseOrdersControllerTests

- `CreatePO_ValidPayload_Returns201` [Integration]
- `CreatePO_InactiveSupplier_Returns409` [Integration]
- `CreatePO_NonExistentSupplier_Returns404` [Integration]
- `CreatePO_InvalidPayload_Returns400ProblemDetails` [Integration]
- `GetPO_ExistingId_Returns200WithLinesAndProgress` [Integration]
- `UpdatePOHeader_DraftPO_Returns200` [Integration]
- `UpdatePOHeader_ConfirmedPO_Returns409` [Integration]
- `AddPOLine_DraftPO_Returns201` [Integration]
- `AddPOLine_ConfirmedPO_Returns409` [Integration]
- `ConfirmPO_DraftPO_Returns200` [Integration]
- `CancelPO_DraftPO_Returns200` [Integration]
- `CancelPO_WithReceipts_Returns409` [Integration]
- `ClosePO_ReceivedPO_Returns200` [Integration]
- `SearchPOs_WithFilters_ReturnsMatchingResults` [Integration]
- `SearchPOs_Pagination_ReturnsCorrectPage` [Integration]

### Integration Tests -- GoodsReceiptsControllerTests

- `CreateReceipt_ValidPayload_Returns201` [Integration]
- `CreateReceipt_CancelledPO_Returns409` [Integration]
- `CreateReceipt_OverReceipt_Returns409` [Integration]
- `CompleteReceipt_PublishesEvent_Returns200` [Integration]
- `CompleteReceipt_UpdatesPOStatus` [Integration]
- `GetReceipt_ExistingId_Returns200WithLines` [Integration]
- `SearchReceipts_WithFilters_ReturnsMatchingResults` [Integration]
- `InspectLine_AcceptPending_Returns200` [Integration]
- `InspectLine_AlreadyInspected_Returns409` [Integration]
- `ResolveQuarantine_AcceptResolution_Returns200` [Integration]

### Integration Tests -- SupplierReturnsControllerTests

- `CreateReturn_ValidPayload_Returns201` [Integration]
- `CreateReturn_InvalidPayload_Returns400ProblemDetails` [Integration]
- `ConfirmReturn_ValidReturn_PublishesEvent_Returns200` [Integration]
- `ConfirmReturn_InsufficientStock_Returns409` [Integration]
- `ConfirmReturn_AlreadyConfirmed_Returns409` [Integration]
- `CancelReturn_DraftReturn_Returns200` [Integration]
- `CancelReturn_ConfirmedReturn_Returns409` [Integration]
- `GetReturn_ExistingId_Returns200WithLines` [Integration]
- `SearchReturns_WithFilters_ReturnsMatchingResults` [Integration]

### Integration Tests -- PurchaseEventsControllerTests

- `ListEvents_ByEventType_Returns200` [Integration]
- `ListEvents_ByEntityTypeAndId_Returns200` [Integration]
- `ListEvents_ByDateRange_Returns200` [Integration]
- `ListEvents_Pagination_ReturnsCorrectPage` [Integration]

---

## 9. Acceptance Criteria

- [ ] Suppliers can be created, updated, searched, deactivated, and reactivated
- [ ] Supplier codes are auto-generated when not provided and are always unique
- [ ] Tax IDs are unique among active suppliers
- [ ] Supplier categories can be managed and assigned to suppliers
- [ ] Contact information (addresses, phones, emails) can be managed per supplier
- [ ] Primary/default promotion occurs automatically when the primary/default item is removed
- [ ] Purchase orders follow the defined status machine with all transitions enforced
- [ ] PO lines reference products from the Inventory catalog (cross-service validation)
- [ ] PO lines can only be modified in Draft status
- [ ] Goods receiving creates receipt records against PO lines with quantity validation
- [ ] Over-receipt is prevented (with optional configurable tolerance)
- [ ] Receiving inspection supports accept/reject/quarantine per receipt line
- [ ] Rejected lines do not count toward PO received totals
- [ ] Quarantined lines can be resolved to accepted or rejected
- [ ] Goods receipt completion publishes MassTransit event for Inventory service
- [ ] Supplier returns publish MassTransit event for Inventory service
- [ ] Immutable purchase event history is maintained for all operations
- [ ] All endpoints require JWT authentication and permission-based authorization (SDD-AUTH-001)
- [ ] All error responses use ProblemDetails (RFC 7807) format
- [ ] All validation rules are enforced with appropriate error codes
- [ ] Health checks include liveness and readiness (DB + RabbitMQ connectivity)
- [ ] Database uses `purchasing` schema with proper cross-schema references

---

## Key Files

- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SuppliersController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SupplierCategoriesController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SupplierContactsController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/PurchaseOrdersController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/PurchaseOrderLinesController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/GoodsReceiptsController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/SupplierReturnsController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Controllers/PurchaseEventsController.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierService.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierCategoryService.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierContactService.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseOrderService.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/GoodsReceiptService.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/ReceivingInspectionService.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/SupplierReturnService.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API/Services/PurchaseEventService.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/Supplier.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/SupplierCategory.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/SupplierAddress.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/SupplierPhone.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/SupplierEmail.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/PurchaseOrder.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/PurchaseOrderLine.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/GoodsReceipt.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/GoodsReceiptLine.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/SupplierReturn.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/SupplierReturnLine.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/Models/PurchaseEvent.cs`
- `src/Databases/Warehouse.Purchasing.DBModel/PurchasingDbContext.cs`
- `src/Warehouse.ServiceModel/DTOs/Purchasing/`
- `src/Warehouse.ServiceModel/Requests/Purchasing/`
- `src/Warehouse.ServiceModel/Events/GoodsReceiptCompletedEvent.cs`
- `src/Warehouse.ServiceModel/Events/SupplierReturnCompletedEvent.cs`
- `src/Warehouse.ServiceModel/Events/GoodsReceiptLineAcceptedEvent.cs`
- `src/Warehouse.Mapping/Profiles/Purchasing/PurchasingMappingProfile.cs`
- `src/Interfaces/Purchasing/Warehouse.Purchasing.API.Tests/`
