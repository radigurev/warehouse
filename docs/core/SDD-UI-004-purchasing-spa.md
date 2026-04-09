# SDD-UI-004 — Purchasing Operations SPA

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Vue.js SPA module for the Purchasing Operations domain. It provides a web-based interface for managing purchase orders, suppliers, supplier categories, goods receiving, receiving inspection, supplier returns, and purchase event history. The SPA consumes the `Warehouse.Purchasing.API` REST endpoints defined in `SDD-PURCH-001`.

**ISA-95 Conformance:** This UI module supports the ISA-95 Part 3 Procurement Operations Activity Model. It enables operators to execute procurement request creation, material receipt recording, receiving inspection, and supplier return workflows through the SPA interface.

**In scope:**
- Purchase order management: list (paginated, filtered), create (with lines), detail view (with receiving progress), edit header/lines (Draft only), confirm, cancel, close
- Purchase order line management: add, edit, remove lines within a PO detail view
- Supplier management: list (paginated, filtered), create, detail (with contacts and category), edit, deactivate (soft-delete), reactivate
- Supplier category management: list, create, edit, delete
- Supplier contact management: addresses, phones, emails within supplier detail view
- Goods receiving: create goods receipt against a PO, list goods receipts, detail view with lines and inspection status
- Receiving inspection: inspect goods receipt lines (accept/reject/quarantine), resolve quarantined lines
- Supplier returns: list (paginated, filtered), create (with lines), detail view, confirm, cancel
- Purchase event history viewer with filtering
- Sidebar navigation group for all Purchasing views
- Form display mode support (modal/page) per `SDD-UI-002`
- Internationalization (English and Bulgarian) per `SDD-UI-001` Section 2.10
- StatusChip display for PO status, inspection status, and supplier return status

**Out of scope:**
- Supplier pricing tiers and contract management (deferred per `SDD-PURCH-001`)
- Automated reorder point calculation (deferred per `SDD-PURCH-001`)
- Supplier performance scoring and analytics (deferred per `SDD-PURCH-001`)
- Three-way matching (PO, receipt, invoice) — deferred to Finance service
- Mobile-responsive layout (desktop-first, per `SDD-UI-001`)
- Dark mode (future enhancement)

**Related specs:**
- `SDD-PURCH-001` — Backend Purchasing API (all endpoints consumed by this module)
- `SDD-UI-001` — Auth Administration SPA (base architecture, shared components, auth flow, i18n, layout)
- `SDD-UI-002` — Form Display Mode (modal vs page rendering for CRUD forms)
- `SDD-AUTH-001` — Authentication and authorization (JWT, permissions)
- `SDD-INV-001` — Products referenced by PO lines (product selector)
- `SDD-INV-003` — Warehouses referenced by POs and goods receipts (warehouse selector)

---

## 2. Behavior

### 2.1 Sidebar Navigation

- The sidebar MUST include a "Purchasing" navigation group below the existing Inventory group.
- The group MUST contain the following navigation items, each with an icon and translated label:
  - Purchase Orders (`mdi-file-document-outline`)
  - Suppliers (`mdi-domain`)
  - Supplier Categories (`mdi-tag-multiple-outline`)
  - Goods Receipts (`mdi-package-down`)
  - Supplier Returns (`mdi-package-up`)
  - Purchase Events (`mdi-history`)
- The active navigation item MUST be visually highlighted based on the current route.
- The group MUST be collapsible (matching existing sidebar group behavior).

### 2.2 Purchase Orders

#### 2.2.1 Purchase Order List

- The SPA MUST display a paginated server-side data table of purchase orders using `v-data-table-server`.
- Columns MUST include: PO Number, Supplier Name, Status, Destination Warehouse, Expected Delivery Date, Total Amount, Created Date.
- The table MUST support column filtering via `ColumnFilter` molecules for: PO Number (text, starts-with), Supplier (autocomplete), Status (select from enum values), Warehouse (select), Date Range (date picker range on Created Date).
- The table MUST support sorting by PO Number, Created Date, Expected Delivery Date, and Supplier Name.
- The default sort MUST be Created Date descending.
- The Status column MUST render a `StatusChip` atom with color coding:
  - `Draft` — grey
  - `Confirmed` — blue
  - `PartiallyReceived` — orange
  - `Received` — green
  - `Closed` — dark grey
  - `Cancelled` — red
- Each row MUST display action chips based on the PO's current status:
  - All statuses: View (detail link)
  - `Draft`: Edit, Confirm, Cancel
  - `Confirmed`: Cancel
  - `PartiallyReceived`: Close
  - `Received`: Close
- The SPA MUST provide a "Create Purchase Order" button that respects `formDisplayMode` (navigate to create page or open create dialog).
- The table MUST respect Vuetify density from the layout store.

#### 2.2.2 Purchase Order Detail View

- The SPA MUST display the full PO details on a dedicated detail page accessible via `/purchasing/purchase-orders/:id`.
- The detail view MUST show:
  - **Header section:** PO Number, Status (StatusChip), Supplier (linked), Destination Warehouse, Expected Delivery Date, Notes, Created By, Created At, Modified At, Confirmed At, Closed At (where applicable).
  - **Lines section:** A data table of PO lines with columns: Product Code, Product Name, Ordered Quantity, Unit Price, Line Total, Received Quantity, Remaining Quantity.
  - **Receiving progress:** Per line, a visual indicator (progress bar or fraction text) showing `Received / Ordered`.
  - **Action buttons:** Status-dependent action buttons in a toolbar:
    - `Draft`: Edit, Confirm, Cancel
    - `Confirmed`: Cancel (only if no receipts), Create Goods Receipt
    - `PartiallyReceived`: Create Goods Receipt, Close
    - `Received`: Close
- Clicking "Edit" MUST navigate to the edit page (or open edit dialog per `formDisplayMode`).
- Clicking "Confirm" MUST show a `ConfirmDialog` before calling `POST /api/v1/purchase-orders/{id}/confirm`.
- Clicking "Cancel" MUST show a `ConfirmDialog` before calling `POST /api/v1/purchase-orders/{id}/cancel`.
- Clicking "Close" MUST show a `ConfirmDialog` before calling `POST /api/v1/purchase-orders/{id}/close`.
- After any status action, the SPA MUST refresh the detail view to reflect the updated status.
- The SPA MUST display a "Back to Purchase Orders" link/button navigating to the PO list.

#### 2.2.3 Create Purchase Order

- The create form MUST include:
  - **Header fields:** Supplier (autocomplete selector, required), Destination Warehouse (select, required), Expected Delivery Date (date picker, optional), Notes (textarea, optional).
  - **Lines section:** An editable table where the user can add one or more lines. Each line MUST have: Product (autocomplete selector), Ordered Quantity (number input), Unit Price (number input), Notes (text input, optional).
- The SPA MUST support adding multiple lines before submitting.
- The SPA MUST support removing a line from the form (client-side only, before submit).
- The SPA MUST calculate and display the line total (Ordered Quantity x Unit Price) per line and a grand total.
- On submit, the SPA MUST call `POST /api/v1/purchase-orders` with the header and all lines in a single request.
- On success, the SPA MUST navigate to the newly created PO's detail page and display a success notification.
- On validation errors, the SPA MUST display inline field errors per the validation rules.
- The SPA MUST prevent submission if no lines have been added (client-side validation, matching backend rule V29).

**Edge cases:**
- If the selected supplier is inactive, the API will return a 409 error — the SPA MUST display the error as a snackbar notification.
- If a selected product is already on the PO (duplicate line), the client SHOULD prevent adding it before submitting. If the API returns a 409, the SPA MUST display the error inline.

#### 2.2.4 Edit Purchase Order

- The edit form MUST only be accessible when the PO is in `Draft` status.
- The SPA MUST pre-populate the form with the PO's current header values and lines.
- Header fields (Supplier excluded — read-only after creation): Destination Warehouse, Expected Delivery Date, Notes.
- The SPA MUST NOT allow changing the Supplier after PO creation.
- Lines can be added, updated, or removed in the edit view.
  - Adding a line MUST call `POST /api/v1/purchase-orders/{poId}/lines`.
  - Updating a line MUST call `PUT /api/v1/purchase-orders/{poId}/lines/{lineId}`.
  - Removing a line MUST show a `ConfirmDialog` before calling `DELETE /api/v1/purchase-orders/{poId}/lines/{lineId}`.
- Updating the header MUST call `PUT /api/v1/purchase-orders/{id}`.
- On success, the SPA MUST navigate back to the PO detail page and display a success notification.
- Attempting to edit a non-Draft PO MUST display a "PO not editable" error.

**Edge cases:**
- Removing the last line of a PO MUST display the API error ("Purchase order must have at least one line").
- If another user confirms the PO while the current user is editing, the next save attempt will return a 409 — the SPA MUST display the error and suggest refreshing.

### 2.3 Suppliers

#### 2.3.1 Supplier List

- The SPA MUST display a paginated server-side data table of suppliers.
- Columns MUST include: Code, Name, Tax ID, Category, Active Status, Created Date.
- The table MUST support column filtering for: Name (text, contains), Code (text, starts-with), Tax ID (text, exact), Category (select), Active status (select: Active/Inactive).
- The default sort MUST be Name ascending.
- Each row MUST display action chips: View, Edit, Deactivate (if active), Reactivate (if inactive).
- The SPA MUST provide a "Create Supplier" button.

#### 2.3.2 Supplier Detail View

- The SPA MUST display the full supplier details on a dedicated detail page accessible via `/purchasing/suppliers/:id`.
- The detail view MUST show:
  - **Header:** Code, Name, Tax ID, Category, Payment Terms (days), Notes, Active status, Created Date.
  - **Addresses tab:** List of addresses with type, full address, default flag. Add/Edit/Delete actions.
  - **Phones tab:** List of phones with type, number, extension, primary flag. Add/Edit/Delete actions.
  - **Emails tab:** List of emails with type, address, primary flag. Add/Edit/Delete actions.
  - **Purchase Orders tab:** List of POs for this supplier (using the PO search endpoint filtered by supplier ID).
- Tabs MUST use Vuetify `v-tabs` component.
- Contact management (add/edit/delete addresses, phones, emails) MUST use dialog forms within the detail view regardless of `formDisplayMode` (these are inline sub-entity forms, not top-level CRUD forms).

#### 2.3.3 Create Supplier

- The create form MUST include: Code (text, optional — auto-generated if blank), Name (text, required), Tax ID (text, optional), Category (select, optional), Payment Terms Days (number, optional), Notes (textarea, optional).
- On success, the SPA MUST navigate to the supplier detail page.

#### 2.3.4 Edit Supplier

- The edit form MUST pre-populate with the supplier's current values.
- The Code field MUST be read-only (not editable after creation).
- On success, the SPA MUST navigate to the supplier detail page.

#### 2.3.5 Deactivate Supplier

- The SPA MUST show a `ConfirmDialog` before calling `DELETE /api/v1/suppliers/{id}`.
- If the supplier has open POs, the API returns a 409 — the SPA MUST display the error message (includes affected PO count).
- On success, the SPA MUST refresh the table and display a success notification.

#### 2.3.6 Reactivate Supplier

- The SPA MUST show a `ConfirmDialog` before calling `POST /api/v1/suppliers/{id}/reactivate`.
- On success, the SPA MUST refresh the table and display a success notification.

### 2.4 Supplier Categories

#### 2.4.1 Supplier Category List

- The SPA MUST display a data table of supplier categories.
- Columns MUST include: Name, Description.
- Each row MUST display action chips: Edit, Delete.
- The SPA MUST provide a "Create Category" button.

#### 2.4.2 Create/Edit Category

- The form MUST include: Name (text, required), Description (textarea, optional).
- On success, the SPA MUST refresh the table and display a success notification.

#### 2.4.3 Delete Category

- The SPA MUST show a `ConfirmDialog` before calling `DELETE /api/v1/supplier-categories/{id}`.
- If the category is in use, the API returns a 409 with the affected supplier count — the SPA MUST display this error.

### 2.5 Goods Receiving

#### 2.5.1 Goods Receipt List

- The SPA MUST display a paginated server-side data table of goods receipts.
- Columns MUST include: Receipt Number, PO Number, Warehouse, Received Date, Created By.
- The table MUST support column filtering for: Receipt Number (text), PO Number (text), Warehouse (select), Date Range (date picker range).
- The default sort MUST be Received Date descending.
- Each row MUST display action chips: View.
- The SPA MUST provide a "Create Goods Receipt" button.

#### 2.5.2 Goods Receipt Detail View

- The SPA MUST display the full goods receipt details on a dedicated page accessible via `/purchasing/goods-receipts/:id`.
- The detail view MUST show:
  - **Header:** Receipt Number, PO Number (linked to PO detail), Warehouse, Location (if specified), Received By, Received Date.
  - **Lines section:** A data table of receipt lines with columns: PO Line (Product Code + Name), Received Quantity, Batch Number, Manufacturing Date, Expiry Date, Inspection Status (StatusChip).
- Inspection status chips MUST use color coding:
  - `Pending` — grey
  - `Accepted` — green
  - `Rejected` — red
  - `Quarantined` — orange
- Each line with `Pending` status MUST display an "Inspect" action chip.
- Each line with `Quarantined` status MUST display a "Resolve" action chip.

#### 2.5.3 Create Goods Receipt

- The create form MUST include:
  - **Header fields:** Purchase Order (autocomplete selector — only POs in `Confirmed` or `PartiallyReceived` status), Warehouse (select, required), Location (select, optional — filtered by selected warehouse).
  - **Lines section:** When a PO is selected, the SPA MUST display the PO's receivable lines (lines with remaining quantity > 0) with: Product (read-only from PO line), Remaining Quantity (read-only), Received Quantity (number input, required), Batch Number (text, optional), Manufacturing Date (date picker, optional), Expiry Date (date picker, optional).
- The SPA MUST prevent entering a received quantity greater than the remaining quantity (client-side validation).
- On submit, the SPA MUST call `POST /api/v1/goods-receipts` with header and lines.
- On success, the SPA MUST navigate to the goods receipt detail page and display a success notification.

#### 2.5.4 Complete Goods Receipt

- The goods receipt detail page MUST display a "Complete" button when the receipt has uninspected lines or when all lines are ready.
- Clicking "Complete" MUST show a `ConfirmDialog` before calling `POST /api/v1/goods-receipts/{id}/complete`.
- On success, the SPA MUST refresh the detail view and display a success notification.

### 2.6 Receiving Inspection

#### 2.6.1 Inspect Receipt Line

- Clicking "Inspect" on a goods receipt line MUST open a dialog with:
  - **Read-only info:** Product, Received Quantity, Batch Number.
  - **Inspection fields:** Status (radio group: Accepted, Rejected, Quarantined), Inspection Note (textarea, optional).
- On submit, the SPA MUST call `POST /api/v1/goods-receipts/{receiptId}/lines/{lineId}/inspect`.
- On success, the SPA MUST refresh the receipt detail and display a success notification.
- The dialog MUST always open as a modal regardless of `formDisplayMode` (inline action, not CRUD form).

#### 2.6.2 Resolve Quarantined Line

- Clicking "Resolve" on a quarantined line MUST open a dialog with:
  - **Read-only info:** Product, Received Quantity, Current Status (Quarantined).
  - **Resolution fields:** Resolution (radio group: Accept, Reject), Note (textarea, optional).
- On submit, the SPA MUST call `POST /api/v1/goods-receipts/{receiptId}/lines/{lineId}/resolve`.
- On success, the SPA MUST refresh the receipt detail and display a success notification.

### 2.7 Supplier Returns

#### 2.7.1 Supplier Return List

- The SPA MUST display a paginated server-side data table of supplier returns.
- Columns MUST include: Return Number, Supplier Name, Status, Reason (truncated), Created Date.
- The table MUST support column filtering for: Return Number (text), Supplier (autocomplete), Status (select: Draft, Confirmed, Cancelled), Date Range.
- The default sort MUST be Created Date descending.
- Status chips MUST use color coding:
  - `Draft` — grey
  - `Confirmed` — blue
  - `Cancelled` — red
- Each row MUST display action chips based on status:
  - `Draft`: View, Confirm, Cancel
  - `Confirmed`: View
  - `Cancelled`: View
- The SPA MUST provide a "Create Supplier Return" button.

#### 2.7.2 Supplier Return Detail View

- The SPA MUST display the full supplier return details on a dedicated page accessible via `/purchasing/supplier-returns/:id`.
- The detail view MUST show:
  - **Header:** Return Number, Supplier (linked), Status (StatusChip), Reason, Notes, Created By, Created Date, Confirmed By, Confirmed Date.
  - **Lines section:** A data table with columns: Product Code, Product Name, Warehouse, Location, Quantity, Goods Receipt Line Reference (if any).
- Action buttons based on status:
  - `Draft`: Confirm, Cancel
  - Others: no actions

#### 2.7.3 Create Supplier Return

- The create form MUST include:
  - **Header fields:** Supplier (autocomplete, required), Reason (text, required), Notes (textarea, optional).
  - **Lines section:** Editable table where the user can add lines with: Product (autocomplete, required), Warehouse (select, required), Location (select, optional — filtered by warehouse), Quantity (number, required), Goods Receipt Line (optional reference).
- On submit, the SPA MUST call `POST /api/v1/supplier-returns` with header and lines.
- On success, the SPA MUST navigate to the supplier return detail page.

### 2.8 Purchase Event History

- The SPA MUST display a paginated server-side data table of purchase events.
- Columns MUST include: Date/Time, Event Type, Entity Type, Entity ID, User, Details (truncated).
- The table MUST support column filtering for: Event Type (select from defined types), Entity Type (select), Date Range.
- Purchase events are read-only — no create, edit, or delete actions.
- Clicking a row or "View" chip SHOULD expand the row or open a dialog showing the full JSON payload.
- Event type filter options MUST include: `PurchaseOrderCreated`, `PurchaseOrderConfirmed`, `PurchaseOrderCancelled`, `PurchaseOrderClosed`, `GoodsReceiptCreated`, `GoodsReceiptCompleted`, `InspectionCompleted`, `SupplierReturnCreated`, `SupplierReturnConfirmed`, `SupplierReturnCancelled`.

### 2.9 Product and Warehouse Selectors

- When creating or editing POs, goods receipts, or supplier returns, the SPA MUST provide autocomplete selectors for cross-domain entities:
  - **Product selector:** Calls Inventory API (`GET /api/v1/products`) with search-as-you-type. Displays Product Code + Name. Returns Product ID.
  - **Warehouse selector:** Calls Inventory API (`GET /api/v1/warehouses`). Displays Warehouse Name. Returns Warehouse ID.
  - **Storage Location selector:** Calls Inventory API (`GET /api/v1/warehouses/{id}/zones/{zoneId}/locations` or equivalent). Filtered by selected warehouse. Displays Location Code. Returns Location ID.
- These selectors MUST debounce API calls (300ms minimum) to avoid excessive requests.

### 2.10 Notifications and Error Handling

- All error handling follows the base patterns defined in `SDD-UI-001` Section 2.8.
- The SPA MUST display success notifications (snackbar) after successful mutations.
- Validation errors (400) MUST be displayed inline on form fields.
- Conflict errors (409) MUST be displayed as snackbar notifications with the API error message.
- Not Found errors (404) MUST display a "Not Found" message with a back link to the list page.
- Loading indicators MUST be shown during all API calls.

---

## 3. Validation Rules

Client-side validation mirrors the backend validation defined in `SDD-PURCH-001` Section 3. The SPA performs client-side validation before submitting to the API, but the API remains the authoritative validator.

### 3.1 Supplier Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V1 | Name | Required. 1-200 characters. | Supplier name is required (max 200 characters). |
| V2 | Code | Optional. 1-20 characters. Alphanumeric + hyphens. | Supplier code must be 1-20 characters (letters, numbers, hyphens). |
| V3 | TaxId | Optional. 1-50 characters when provided. | Tax ID must be under 50 characters. |
| V4 | PaymentTermDays | Optional. 0-365 when provided. | Payment terms must be between 0 and 365 days. |
| V5 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |

### 3.2 Supplier Category Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V6 | Name | Required. 1-100 characters. | Category name is required (max 100 characters). |
| V7 | Description | Optional. Max 500 characters. | Description must be under 500 characters. |

### 3.3 Supplier Address Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V8 | AddressType | Required. One of: Billing, Shipping, Both. | Please select an address type. |
| V9 | StreetLine1 | Required. 1-200 characters. | Street address is required. |
| V10 | StreetLine2 | Optional. Max 200 characters. | Street line 2 must be under 200 characters. |
| V11 | City | Required. 1-100 characters. | City is required. |
| V12 | StateProvince | Optional. Max 100 characters. | State/province must be under 100 characters. |
| V13 | PostalCode | Required. 1-20 characters. | Postal code is required. |
| V14 | CountryCode | Required. 2-letter ISO code. | Please enter a valid 2-letter country code. |

### 3.4 Supplier Phone Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V15 | PhoneType | Required. One of: Mobile, Landline, Fax. | Please select a phone type. |
| V16 | PhoneNumber | Required. 5-20 characters. Digits, spaces, hyphens, parentheses, leading +. | Phone number must be 5-20 characters. |
| V17 | Extension | Optional. Max 10 characters. Digits only. | Extension must be digits only (max 10). |

### 3.5 Supplier Email Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V18 | EmailType | Required. One of: General, Billing, Support. | Please select an email type. |
| V19 | EmailAddress | Required. Valid email format. Max 256 characters. | Please enter a valid email address. |

### 3.6 Purchase Order Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V20 | SupplierId | Required. | Please select a supplier. |
| V21 | DestinationWarehouseId | Required. | Please select a destination warehouse. |
| V22 | ExpectedDeliveryDate | Optional. Must be today or future. | Expected delivery date must be today or a future date. |
| V23 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |
| V24 | Lines | At least one line required. | Purchase order must have at least one line. |

### 3.7 Purchase Order Line Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V25 | ProductId | Required. | Please select a product. |
| V26 | OrderedQuantity | Required. Must be > 0. | Quantity must be greater than zero. |
| V27 | UnitPrice | Required. Must be >= 0. | Unit price must be zero or greater. |
| V28 | Notes | Optional. Max 500 characters. | Notes must be under 500 characters. |

### 3.8 Goods Receipt Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V29 | PurchaseOrderId | Required. | Please select a purchase order. |
| V30 | WarehouseId | Required. | Please select a warehouse. |
| V31 | LocationId | Optional. | — |

### 3.9 Goods Receipt Line Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V32 | ReceivedQuantity | Required. Must be > 0. | Received quantity must be greater than zero. |
| V33 | ReceivedQuantity | Must not exceed remaining quantity. | Received quantity exceeds remaining quantity on PO line. |
| V34 | BatchNumber | Optional. Max 50 characters. | Batch number must be under 50 characters. |
| V35 | ManufacturingDate | Optional. Must not be in the future. | Manufacturing date cannot be in the future. |
| V36 | ExpiryDate | Optional. Must be after manufacturing date when both provided. | Expiry date must be after manufacturing date. |

### 3.10 Receiving Inspection Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V37 | InspectionStatus | Required. One of: Accepted, Rejected, Quarantined. | Please select an inspection result. |
| V38 | InspectionNote | Optional. Max 2000 characters. | Inspection note must be under 2000 characters. |

### 3.11 Supplier Return Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V39 | SupplierId | Required. | Please select a supplier. |
| V40 | Reason | Required. 1-500 characters. | Reason is required (max 500 characters). |
| V41 | Notes | Optional. Max 2000 characters. | Notes must be under 2000 characters. |
| V42 | Lines | At least one line required. | Supplier return must have at least one line. |

### 3.12 Supplier Return Line Forms

| # | Field | Rule | Error Message |
|---|---|---|---|
| V43 | ProductId | Required. | Please select a product. |
| V44 | WarehouseId | Required. | Please select a warehouse. |
| V45 | Quantity | Required. Must be > 0. | Quantity must be greater than zero. |

---

## 4. Error Rules

All error handling maps to the ProblemDetails responses defined in `SDD-PURCH-001` Section 4.

| # | API Error Code | HTTP Status | UI Behavior |
|---|---|---|---|
| **Supplier errors** | | | |
| E1 | `SUPPLIER_NOT_FOUND` | 404 | Display "Supplier not found" — snackbar or Not Found page. |
| E2 | `DUPLICATE_SUPPLIER_CODE` | 409 | Display inline error on Code field: "A supplier with this code already exists." |
| E3 | `DUPLICATE_TAX_ID` | 409 | Display inline error on Tax ID field: "An active supplier with this tax ID already exists." |
| E4 | `INVALID_CATEGORY` | 400 | Display inline error on Category field: "The selected category does not exist." |
| E5 | `CATEGORY_NOT_FOUND` | 404 | Display snackbar: "Supplier category not found." Refresh list. |
| E6 | `CATEGORY_IN_USE` | 409 | Display snackbar with API message (includes supplier count). |
| E7 | `DUPLICATE_CATEGORY_NAME` | 409 | Display inline error on Name field: "A category with this name already exists." |
| E8 | `SUPPLIER_HAS_OPEN_POS` | 409 | Display snackbar with API message (includes PO count). |
| E9 | `SUPPLIER_INACTIVE` | 409 | Display snackbar: "The supplier is inactive and cannot be used for new purchase orders." |
| **Contact errors** | | | |
| E10 | `ADDRESS_NOT_FOUND` | 404 | Display snackbar: "Address not found." Refresh contact list. |
| E11 | `PHONE_NOT_FOUND` | 404 | Display snackbar: "Phone not found." Refresh contact list. |
| E12 | `EMAIL_NOT_FOUND` | 404 | Display snackbar: "Email not found." Refresh contact list. |
| E13 | `DUPLICATE_SUPPLIER_EMAIL` | 409 | Display inline error on Email field: "This supplier already has this email address." |
| **Purchase Order errors** | | | |
| E14 | `PO_NOT_FOUND` | 404 | Display Not Found page with link to PO list. |
| E15 | `PO_LINE_NOT_FOUND` | 404 | Display snackbar: "Purchase order line not found." Refresh detail. |
| E16 | `PO_NOT_EDITABLE` | 409 | Display snackbar: "Purchase order can only be edited in Draft status." |
| E17 | `INVALID_PO_STATUS_TRANSITION` | 409 | Display snackbar with API message (includes from/to statuses). |
| E18 | `PO_MUST_HAVE_LINES` | 409 | Display snackbar: "Purchase order must have at least one line." |
| E19 | `DUPLICATE_PO_LINE` | 409 | Display inline error: "This product is already on the purchase order." |
| E20 | `PO_HAS_RECEIPTS` | 409 | Display snackbar: "Cannot cancel — goods have been received." |
| E21 | `PO_ALREADY_CLOSED` | 409 | Display snackbar: "Purchase order is already closed." Refresh detail. |
| E22 | `INVALID_PRODUCT` | 400 | Display inline error on Product field: "The selected product does not exist or is inactive." |
| E23 | `INVALID_WAREHOUSE` | 400 | Display inline error on Warehouse field: "The selected warehouse does not exist or is inactive." |
| **Goods Receipt errors** | | | |
| E24 | `RECEIPT_NOT_FOUND` | 404 | Display Not Found page with link to receipts list. |
| E25 | `PO_NOT_RECEIVABLE` | 409 | Display snackbar: "Purchase order is not in a receivable status." |
| E26 | `OVER_RECEIPT` | 409 | Display inline error on Received Quantity: "Received quantity exceeds remaining quantity." |
| E27 | `LINE_FULLY_RECEIVED` | 409 | Display snackbar: "This PO line has already been fully received." |
| E28 | `INVALID_PO_LINE` | 400 | Display snackbar: "Invalid PO line reference." |
| E29 | `INVALID_LOCATION` | 400 | Display inline error on Location field: "The selected location is not valid for this warehouse." |
| **Inspection errors** | | | |
| E30 | `LINE_ALREADY_INSPECTED` | 409 | Display snackbar: "This line has already been inspected." Refresh detail. |
| E31 | `RECEIPT_LINE_NOT_FOUND` | 404 | Display snackbar: "Goods receipt line not found." |
| **Supplier Return errors** | | | |
| E32 | `RETURN_NOT_FOUND` | 404 | Display Not Found page with link to returns list. |
| E33 | `RETURN_ALREADY_CONFIRMED` | 409 | Display snackbar: "Supplier return has already been confirmed." Refresh detail. |
| E34 | `INSUFFICIENT_STOCK` | 409 | Display snackbar: "Insufficient stock at the specified location." |
| E35 | `RETURN_MUST_HAVE_LINES` | 400 | Display inline error: "Supplier return must have at least one line." |
| **Common errors** | | | |
| E36 | `VALIDATION_ERROR` | 400 | Map `errors` object from ProblemDetails to inline field errors. |
| E37 | `FORBIDDEN` | 403 | Display snackbar: "You do not have permission to perform this action." |
| E38 | `UNAUTHORIZED` | 401 | Trigger token refresh per `SDD-UI-001` Section 2.1. |
| E39 | Network error | — | Display snackbar: "Unable to connect to the server. Please check your connection." |
| E40 | Unhandled 500 | 500 | Display snackbar: "An unexpected error occurred. Please try again." |

---

## 5. Routes

| Route | View | Auth Required | Description |
|---|---|---|---|
| `/purchasing/purchase-orders` | PurchaseOrdersView | Yes | PO list with filtering |
| `/purchasing/purchase-orders/create` | PurchaseOrderCreatePage | Yes | Create PO with lines |
| `/purchasing/purchase-orders/:id` | PurchaseOrderDetailView | Yes | PO detail with lines and progress |
| `/purchasing/purchase-orders/:id/edit` | PurchaseOrderEditPage | Yes | Edit PO header and lines |
| `/purchasing/suppliers` | SuppliersView | Yes | Supplier list with filtering |
| `/purchasing/suppliers/create` | SupplierCreatePage | Yes | Create supplier |
| `/purchasing/suppliers/:id` | SupplierDetailView | Yes | Supplier detail with contacts |
| `/purchasing/suppliers/:id/edit` | SupplierEditPage | Yes | Edit supplier |
| `/purchasing/supplier-categories` | SupplierCategoriesView | Yes | Supplier category list |
| `/purchasing/supplier-categories/create` | SupplierCategoryCreatePage | Yes | Create category |
| `/purchasing/supplier-categories/:id/edit` | SupplierCategoryEditPage | Yes | Edit category |
| `/purchasing/goods-receipts` | GoodsReceiptsView | Yes | Goods receipt list |
| `/purchasing/goods-receipts/create` | GoodsReceiptCreatePage | Yes | Create goods receipt |
| `/purchasing/goods-receipts/:id` | GoodsReceiptDetailView | Yes | Receipt detail with inspection |
| `/purchasing/supplier-returns` | SupplierReturnsView | Yes | Supplier return list |
| `/purchasing/supplier-returns/create` | SupplierReturnCreatePage | Yes | Create supplier return |
| `/purchasing/supplier-returns/:id` | SupplierReturnDetailView | Yes | Return detail |
| `/purchasing/purchase-events` | PurchaseEventsView | Yes | Purchase event history |

---

## 6. Versioning Notes

- **v1 — Initial specification (2026-04-09)**
  - Purchase order lifecycle management (list, create with lines, detail, edit, confirm, cancel, close)
  - Supplier management (list, create, detail with contacts, edit, deactivate, reactivate)
  - Supplier categories (list, create, edit, delete)
  - Goods receiving (create against PO, list, detail)
  - Receiving inspection (accept, reject, quarantine, resolve quarantined)
  - Supplier returns (list, create with lines, detail, confirm, cancel)
  - Purchase event history viewer
  - Client-side validation mirroring `SDD-PURCH-001` rules
  - StatusChip display for all status-driven entities
  - Form display mode support per `SDD-UI-002`
  - i18n: English and Bulgarian

---

## 7. Test Plan

### Unit Tests — Purchase Order List

- `purchaseOrdersView_LoadsTable_DisplaysColumns` [Unit]
- `purchaseOrdersView_FilterByStatus_FiltersResults` [Unit]
- `purchaseOrdersView_FilterBySupplier_FiltersResults` [Unit]
- `purchaseOrdersView_DefaultSort_CreatedDateDescending` [Unit]
- `purchaseOrdersView_StatusChip_DisplaysCorrectColor` [Unit]
- `purchaseOrdersView_ActionChips_DraftShowsEditConfirmCancel` [Unit]
- `purchaseOrdersView_ActionChips_ConfirmedShowsCancel` [Unit]
- `purchaseOrdersView_ActionChips_PartiallyReceivedShowsClose` [Unit]
- `purchaseOrdersView_CreateButton_RespectsFormDisplayMode` [Unit]

### Unit Tests — Purchase Order Detail

- `purchaseOrderDetail_LoadsPOWithLines` [Unit]
- `purchaseOrderDetail_DisplaysReceivingProgress` [Unit]
- `purchaseOrderDetail_DraftStatus_ShowsEditConfirmCancelButtons` [Unit]
- `purchaseOrderDetail_ConfirmedStatus_ShowsCancelAndCreateReceiptButtons` [Unit]
- `purchaseOrderDetail_PartiallyReceivedStatus_ShowsCreateReceiptAndCloseButtons` [Unit]
- `purchaseOrderDetail_ReceivedStatus_ShowsCloseButton` [Unit]
- `purchaseOrderDetail_ClosedStatus_ShowsNoActionButtons` [Unit]
- `purchaseOrderDetail_ConfirmAction_ShowsConfirmDialog` [Unit]
- `purchaseOrderDetail_CancelAction_ShowsConfirmDialog` [Unit]
- `purchaseOrderDetail_CloseAction_ShowsConfirmDialog` [Unit]

### Unit Tests — Purchase Order Create

- `purchaseOrderCreate_EmptySupplier_ShowsValidationError` [Unit]
- `purchaseOrderCreate_EmptyWarehouse_ShowsValidationError` [Unit]
- `purchaseOrderCreate_NoLines_ShowsValidationError` [Unit]
- `purchaseOrderCreate_LineMissingProduct_ShowsError` [Unit]
- `purchaseOrderCreate_LineZeroQuantity_ShowsError` [Unit]
- `purchaseOrderCreate_LineNegativePrice_ShowsError` [Unit]
- `purchaseOrderCreate_CalculatesLineTotals` [Unit]
- `purchaseOrderCreate_CalculatesGrandTotal` [Unit]
- `purchaseOrderCreate_AddRemoveLines_UpdatesTotal` [Unit]
- `purchaseOrderCreate_DuplicateProduct_ShowsError` [Unit]
- `purchaseOrderCreate_PastDeliveryDate_ShowsError` [Unit]
- `purchaseOrderCreate_SuccessfulSubmit_NavigatesToDetail` [Unit]

### Unit Tests — Purchase Order Edit

- `purchaseOrderEdit_PrePopulatesHeaderFields` [Unit]
- `purchaseOrderEdit_PrePopulatesLines` [Unit]
- `purchaseOrderEdit_SupplierFieldReadOnly` [Unit]
- `purchaseOrderEdit_NonDraftPO_ShowsNotEditableError` [Unit]
- `purchaseOrderEdit_AddLine_CallsApi` [Unit]
- `purchaseOrderEdit_RemoveLine_ShowsConfirmDialog` [Unit]
- `purchaseOrderEdit_RemoveLastLine_ShowsError` [Unit]

### Unit Tests — Supplier List

- `suppliersView_LoadsTable_DisplaysColumns` [Unit]
- `suppliersView_FilterByName_FiltersResults` [Unit]
- `suppliersView_FilterByActiveStatus_FiltersResults` [Unit]
- `suppliersView_DefaultSort_NameAscending` [Unit]
- `suppliersView_DeactivateChip_ShowsConfirmDialog` [Unit]
- `suppliersView_ReactivateChip_ShowsConfirmDialog` [Unit]

### Unit Tests — Supplier Detail

- `supplierDetail_LoadsSupplierWithContacts` [Unit]
- `supplierDetail_DisplaysTabs_AddressesPhoneEmails` [Unit]
- `supplierDetail_AddAddress_OpensDialog` [Unit]
- `supplierDetail_EditAddress_OpensDialogWithData` [Unit]
- `supplierDetail_DeleteAddress_ShowsConfirmDialog` [Unit]
- `supplierDetail_PurchaseOrdersTab_DisplaysFilteredPOs` [Unit]

### Unit Tests — Supplier Create/Edit

- `supplierCreate_EmptyName_ShowsError` [Unit]
- `supplierCreate_CodeOverMaxLength_ShowsError` [Unit]
- `supplierCreate_PaymentTermsNegative_ShowsError` [Unit]
- `supplierCreate_PaymentTermsOver365_ShowsError` [Unit]
- `supplierCreate_SuccessfulSubmit_NavigatesToDetail` [Unit]
- `supplierEdit_PrePopulatesFields` [Unit]
- `supplierEdit_CodeFieldReadOnly` [Unit]

### Unit Tests — Supplier Categories

- `supplierCategoriesView_LoadsTable` [Unit]
- `supplierCategoryCreate_EmptyName_ShowsError` [Unit]
- `supplierCategoryDelete_ShowsConfirmDialog` [Unit]
- `supplierCategoryDelete_InUse_ShowsErrorWithCount` [Unit]

### Unit Tests — Goods Receipts

- `goodsReceiptsView_LoadsTable_DisplaysColumns` [Unit]
- `goodsReceiptDetail_LoadsReceiptWithLines` [Unit]
- `goodsReceiptDetail_PendingLine_ShowsInspectChip` [Unit]
- `goodsReceiptDetail_QuarantinedLine_ShowsResolveChip` [Unit]
- `goodsReceiptDetail_InspectionStatusChip_DisplaysCorrectColor` [Unit]
- `goodsReceiptCreate_SelectPO_LoadsReceivableLines` [Unit]
- `goodsReceiptCreate_QuantityExceedsRemaining_ShowsError` [Unit]
- `goodsReceiptCreate_FutureManufacturingDate_ShowsError` [Unit]
- `goodsReceiptCreate_ExpiryBeforeManufacturing_ShowsError` [Unit]
- `goodsReceiptCreate_SuccessfulSubmit_NavigatesToDetail` [Unit]

### Unit Tests — Receiving Inspection

- `inspectDialog_ShowsProductAndQuantity` [Unit]
- `inspectDialog_NoStatusSelected_ShowsError` [Unit]
- `inspectDialog_SuccessfulSubmit_RefreshesDetail` [Unit]
- `resolveDialog_ShowsCurrentQuarantinedStatus` [Unit]
- `resolveDialog_SuccessfulResolve_RefreshesDetail` [Unit]

### Unit Tests — Supplier Returns

- `supplierReturnsView_LoadsTable_DisplaysColumns` [Unit]
- `supplierReturnsView_StatusChip_DisplaysCorrectColor` [Unit]
- `supplierReturnDetail_LoadsReturnWithLines` [Unit]
- `supplierReturnDetail_DraftStatus_ShowsConfirmCancelButtons` [Unit]
- `supplierReturnCreate_EmptySupplier_ShowsError` [Unit]
- `supplierReturnCreate_EmptyReason_ShowsError` [Unit]
- `supplierReturnCreate_NoLines_ShowsError` [Unit]
- `supplierReturnCreate_SuccessfulSubmit_NavigatesToDetail` [Unit]

### Unit Tests — Purchase Events

- `purchaseEventsView_LoadsTable_DisplaysColumns` [Unit]
- `purchaseEventsView_FilterByEventType_FiltersResults` [Unit]
- `purchaseEventsView_ExpandRow_ShowsJsonPayload` [Unit]

### Unit Tests — Validation

- `supplierForm_NameOverMaxLength_ShowsError` [Unit]
- `supplierAddressForm_MissingCity_ShowsError` [Unit]
- `supplierPhoneForm_ShortNumber_ShowsError` [Unit]
- `supplierEmailForm_InvalidFormat_ShowsError` [Unit]
- `purchaseOrderForm_MissingSupplier_ShowsError` [Unit]
- `goodsReceiptForm_MissingPO_ShowsError` [Unit]

### Unit Tests — i18n

- `i18n_PurchasingLabels_TranslatedInEnglish` [Unit]
- `i18n_PurchasingLabels_TranslatedInBulgarian` [Unit]
- `i18n_PurchasingPageTitles_TranslatedInBulgarian` [Unit]
- `i18n_PurchasingStatusLabels_TranslatedInBulgarian` [Unit]
- `i18n_PurchasingValidationMessages_TranslatedInBulgarian` [Unit]

### Integration Tests — Purchase Order Flows

- `purchaseOrderList_LoadsAndDisplaysOrders` [Integration]
- `purchaseOrderCreate_SubmitsAndRedirectsToDetail` [Integration]
- `purchaseOrderDetail_ConfirmAction_UpdatesStatus` [Integration]
- `purchaseOrderDetail_CancelAction_UpdatesStatus` [Integration]
- `purchaseOrderDetail_CloseAction_UpdatesStatus` [Integration]
- `purchaseOrderEdit_UpdatesHeaderAndRefreshes` [Integration]
- `purchaseOrderEdit_AddLine_RefreshesLines` [Integration]
- `purchaseOrderEdit_RemoveLine_RefreshesLines` [Integration]

### Integration Tests — Supplier Flows

- `supplierList_LoadsAndDisplaysSuppliers` [Integration]
- `supplierCreate_SubmitsAndRedirectsToDetail` [Integration]
- `supplierDetail_DeactivateAction_ShowsConfirmAndUpdates` [Integration]
- `supplierDetail_DeactivateWithOpenPOs_ShowsError` [Integration]
- `supplierDetail_AddAddress_AppearsInList` [Integration]
- `supplierDetail_ReactivateAction_UpdatesStatus` [Integration]

### Integration Tests — Goods Receipt Flows

- `goodsReceiptCreate_SelectsPO_LoadsLines_Submits` [Integration]
- `goodsReceiptDetail_InspectLine_UpdatesStatus` [Integration]
- `goodsReceiptDetail_ResolveQuarantined_UpdatesStatus` [Integration]
- `goodsReceiptDetail_CompleteReceipt_UpdatesPOStatus` [Integration]

### Integration Tests — Supplier Return Flows

- `supplierReturnCreate_SubmitsAndRedirectsToDetail` [Integration]
- `supplierReturnDetail_ConfirmAction_UpdatesStatus` [Integration]
- `supplierReturnDetail_InsufficientStock_ShowsError` [Integration]
- `supplierReturnDetail_CancelDraft_UpdatesStatus` [Integration]

### Integration Tests — Navigation

- `purchasingSidebar_AllLinksNavigateCorrectly` [Integration]
- `purchaseOrderDetail_BackButton_NavigatesToList` [Integration]
- `routeGuard_UnauthenticatedAccess_RedirectsToLogin` [Integration]

---

## Key Files

- `frontend/src/features/purchasing/api/purchase-orders.ts` (new)
- `frontend/src/features/purchasing/api/suppliers.ts` (new)
- `frontend/src/features/purchasing/api/supplier-categories.ts` (new)
- `frontend/src/features/purchasing/api/supplier-contacts.ts` (new)
- `frontend/src/features/purchasing/api/goods-receipts.ts` (new)
- `frontend/src/features/purchasing/api/supplier-returns.ts` (new)
- `frontend/src/features/purchasing/api/purchase-events.ts` (new)
- `frontend/src/features/purchasing/types/purchasing.ts` (new)
- `frontend/src/features/purchasing/composables/usePurchaseOrdersView.ts` (new)
- `frontend/src/features/purchasing/composables/usePurchaseOrderDetailView.ts` (new)
- `frontend/src/features/purchasing/composables/useSuppliersView.ts` (new)
- `frontend/src/features/purchasing/composables/useSupplierDetailView.ts` (new)
- `frontend/src/features/purchasing/composables/useSupplierCategoriesView.ts` (new)
- `frontend/src/features/purchasing/composables/useGoodsReceiptsView.ts` (new)
- `frontend/src/features/purchasing/composables/useGoodsReceiptDetailView.ts` (new)
- `frontend/src/features/purchasing/composables/useSupplierReturnsView.ts` (new)
- `frontend/src/features/purchasing/composables/useSupplierReturnDetailView.ts` (new)
- `frontend/src/features/purchasing/composables/usePurchaseEventsView.ts` (new)
- `frontend/src/features/purchasing/views/purchase-orders/PurchaseOrdersView.vue` (new)
- `frontend/src/features/purchasing/views/purchase-orders/PurchaseOrderCreatePage.vue` (new)
- `frontend/src/features/purchasing/views/purchase-orders/PurchaseOrderDetailView.vue` (new)
- `frontend/src/features/purchasing/views/purchase-orders/PurchaseOrderEditPage.vue` (new)
- `frontend/src/features/purchasing/views/suppliers/SuppliersView.vue` (new)
- `frontend/src/features/purchasing/views/suppliers/SupplierCreatePage.vue` (new)
- `frontend/src/features/purchasing/views/suppliers/SupplierDetailView.vue` (new)
- `frontend/src/features/purchasing/views/suppliers/SupplierEditPage.vue` (new)
- `frontend/src/features/purchasing/views/supplier-categories/SupplierCategoriesView.vue` (new)
- `frontend/src/features/purchasing/views/supplier-categories/SupplierCategoryCreatePage.vue` (new)
- `frontend/src/features/purchasing/views/supplier-categories/SupplierCategoryEditPage.vue` (new)
- `frontend/src/features/purchasing/views/goods-receipts/GoodsReceiptsView.vue` (new)
- `frontend/src/features/purchasing/views/goods-receipts/GoodsReceiptCreatePage.vue` (new)
- `frontend/src/features/purchasing/views/goods-receipts/GoodsReceiptDetailView.vue` (new)
- `frontend/src/features/purchasing/views/supplier-returns/SupplierReturnsView.vue` (new)
- `frontend/src/features/purchasing/views/supplier-returns/SupplierReturnCreatePage.vue` (new)
- `frontend/src/features/purchasing/views/supplier-returns/SupplierReturnDetailView.vue` (new)
- `frontend/src/features/purchasing/views/purchase-events/PurchaseEventsView.vue` (new)
- `frontend/src/features/purchasing/components/organisms/PurchaseOrderFormDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/SupplierFormDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/SupplierCategoryFormDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/GoodsReceiptFormDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/InspectionDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/QuarantineResolveDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/SupplierReturnFormDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/SupplierAddressFormDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/SupplierPhoneFormDialog.vue` (new)
- `frontend/src/features/purchasing/components/organisms/SupplierEmailFormDialog.vue` (new)
- `frontend/src/app/router/purchasing.routes.ts` (new)
- `frontend/src/shared/i18n/locales/en.ts` (modified — add purchasing keys)
- `frontend/src/shared/i18n/locales/bg.ts` (modified — add purchasing keys)
- `frontend/src/shared/components/templates/DefaultLayout.vue` (modified — add purchasing nav group)
