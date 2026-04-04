# SDD-CUST-001 — Customers and Accounts

> Status: Implemented
> Last updated: 2026-04-05
> Owner: TBD
> Category: Domain

## 1. Context & Scope

This spec defines the Customer domain for the Warehouse system. It covers customer lifecycle management, multi-currency account management, contact information (addresses, phones, emails), customer categorization, and account merging. Customers are a foundational domain entity referenced by orders, invoices, payments, and nearly all downstream services.

**In scope:**
- Customer CRUD (create, read, update, soft-delete/deactivate)
- Customer search and filtering with pagination
- Customer account management (multi-currency accounts per customer)
- Contact information management (addresses, phones, emails)
- Customer category management and assignment
- Account merge (consolidate duplicate customer accounts)
- Audit columns on all entities (CreatedAtUtc, ModifiedAtUtc)

**Out of scope:**
- Customer-facing self-service portal (no public-facing endpoints)
- Credit limit management (future enhancement, likely part of Finance service)
- Customer pricing or discount rules (covered by Finance service — see microservices plan)
- Order or invoice history (covered by Orders and Finance services)
- Customer import/export (future enhancement)

**Related specs:**
- `SDD-AUTH-001` — All endpoints require JWT authentication and permission-based authorization.

---

## 2. Behavior

### 2.1 Customer Management

#### 2.1.1 Create Customer

- The system MUST support creating a customer with a name, code, tax ID, customer category, and active status.
- The system MUST generate a unique customer code if one is not provided. Generated codes MUST follow the format `CUST-{sequential number padded to 6 digits}` (e.g., `CUST-000001`).
- The system MUST enforce unique customer codes across all customers (including soft-deleted).
- The system MUST enforce unique tax IDs across all active customers. Two soft-deleted customers MAY share a tax ID.
- The system MUST set `IsActive = true` by default on creation.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.
- The system MUST return the created customer with its generated ID and code.

**Edge cases:**
- Creating a customer with a duplicate code MUST return a 409 Conflict error.
- Creating a customer with a duplicate tax ID (among active customers) MUST return a 409 Conflict error.
- Creating a customer with an invalid (non-existent) category ID MUST return a 400 Validation error.

#### 2.1.2 Update Customer

- The system MUST support updating customer fields: name, tax ID, category, and notes.
- The system MUST NOT allow changing the customer code after creation.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on every update.
- Updating a soft-deleted customer MUST return a 404 Not Found error.

**Edge cases:**
- Updating the tax ID to one that already belongs to another active customer MUST return a 409 Conflict error.
- Updating a customer with a non-existent category ID MUST return a 400 Validation error.

#### 2.1.3 Get Customer

- The system MUST support retrieving a single customer by ID, including nested contact information, accounts, and category.
- Retrieving a soft-deleted customer MUST return a 404 Not Found error.

#### 2.1.4 Search Customers

- The system MUST support paginated listing of customers with configurable page size and page number.
- The system MUST support filtering by: name (contains), code (exact or starts-with), tax ID (exact), category ID, and active status.
- The system MUST support sorting by name, code, and creation date.
- The system MUST default to sorting by name ascending when no sort is specified.
- The system MUST exclude soft-deleted customers from search results by default.
- The system SHOULD support a query parameter to include soft-deleted customers for administrative purposes.

#### 2.1.5 Deactivate Customer (Soft Delete)

- The system MUST support soft-deleting a customer by setting `IsDeleted = true` and `DeletedAtUtc = current UTC time`.
- Soft-deleted customers MUST NOT appear in standard queries or searches.
- Soft-deleted customers MUST NOT be updatable.
- The system MUST set `IsActive = false` when a customer is soft-deleted.
- The system SHOULD prevent deactivation if the customer has active (non-zero balance) accounts. This check SHOULD return a 409 Conflict error with details of the blocking accounts.

**Edge cases:**
- Deactivating an already soft-deleted customer MUST return a 404 Not Found error.
- Deactivating a customer with active accounts SHOULD return a 409 Conflict error.

#### 2.1.6 Reactivate Customer

- The system MUST support reactivating a soft-deleted customer by setting `IsDeleted = false`, `DeletedAtUtc = null`, and `IsActive = true`.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on reactivation.
- Reactivating a customer whose code or tax ID now conflicts with another active customer MUST return a 409 Conflict error.

### 2.2 Customer Accounts

#### 2.2.1 Create Account

- The system MUST support creating accounts for a customer with a currency code and an optional description.
- The system MUST enforce that each customer has at most one account per currency code.
- The system MUST set the initial balance to `0.00` on account creation.
- The system MUST mark the first account created for a customer as the primary account (`IsPrimary = true`).
- The system MUST record `CreatedAtUtc` on account creation.

**Edge cases:**
- Creating a duplicate account for the same customer and currency MUST return a 409 Conflict error.
- Creating an account for a non-existent or soft-deleted customer MUST return a 404 Not Found error.
- Creating an account with an unsupported currency code MUST return a 400 Validation error.

#### 2.2.2 Update Account

- The system MUST support updating the account description and primary flag.
- When setting an account as primary (`IsPrimary = true`), the system MUST unset the previous primary account for that customer.
- The system MUST NOT allow changing the currency code after creation.

#### 2.2.3 List Accounts

- The system MUST support listing all accounts for a specific customer.
- The response MUST include account ID, currency code, balance, description, primary flag, and creation date.

#### 2.2.4 Deactivate Account

- The system MUST support deactivating (soft-deleting) an account.
- The system MUST prevent deactivation of an account with a non-zero balance. This MUST return a 409 Conflict error.
- The system MUST prevent deactivation of the sole remaining active account for a customer. This MUST return a 409 Conflict error.

#### 2.2.5 Account Merge

- The system MUST support merging two customer accounts with the same currency code belonging to the same customer.
- The merge MUST transfer the balance from the source account to the target account.
- The merge MUST soft-delete the source account after transfer.
- The system MUST NOT allow merging accounts with different currency codes. This MUST return a 400 Validation error.
- The system MUST NOT allow merging an account into itself. This MUST return a 400 Validation error.
- The system MUST NOT allow merging accounts that belong to different customers. This MUST return a 400 Validation error.
- The system MUST NOT allow merging into an inactive (soft-deleted) target account. This MUST return a 404 Not Found error.
- The merge operation MUST be performed within a single database transaction.
- The system MUST record the merge details (source account ID, target account ID, transferred amount) in an audit trail.

**Edge cases:**
- Merging accounts with different currencies MUST return a 400 Validation error.
- Merging an account into itself MUST return a 400 Validation error.
- Merging into a soft-deleted target MUST return a 404 Not Found error.

### 2.3 Contact Information — Addresses

#### 2.3.1 Create Address

- The system MUST support creating addresses for a customer with: address type (billing, shipping, both), street line 1, street line 2 (optional), city, state/province (optional), postal code, and country code.
- The system MUST mark the first address of each type as the default for that type.
- The system MUST record `CreatedAtUtc` on creation.

#### 2.3.2 Update Address

- The system MUST support updating all address fields except the customer association.
- The system MUST update `ModifiedAtUtc` on every update.

#### 2.3.3 Delete Address

- The system MUST support deleting an address.
- If the deleted address was the default, the system SHOULD automatically promote the next most recently created address of the same type to default.
- The system MUST NOT delete the last address of a given type if the customer has active orders referencing that address type. This MAY be deferred to the Orders service integration.

#### 2.3.4 List Addresses

- The system MUST support listing all addresses for a customer.
- The system SHOULD support filtering by address type.

### 2.4 Contact Information — Phones

#### 2.4.1 Create Phone

- The system MUST support creating phone entries for a customer with: phone type (mobile, landline, fax), phone number, and extension (optional).
- The system MUST mark the first phone as the primary phone.

#### 2.4.2 Update Phone

- The system MUST support updating phone type, number, extension, and primary flag.
- When setting a phone as primary, the system MUST unset the previous primary phone.

#### 2.4.3 Delete Phone

- The system MUST support deleting a phone entry.
- If the deleted phone was primary, the system SHOULD automatically promote the next phone to primary.

#### 2.4.4 List Phones

- The system MUST support listing all phones for a customer.

### 2.5 Contact Information — Emails

#### 2.5.1 Create Email

- The system MUST support creating email entries for a customer with: email type (general, billing, support), email address.
- The system MUST enforce unique email addresses per customer (a customer cannot have the same email address twice).
- The system MUST mark the first email as the primary email.

#### 2.5.2 Update Email

- The system MUST support updating email type, address, and primary flag.
- When setting an email as primary, the system MUST unset the previous primary email.
- The system MUST enforce uniqueness of the email address within the same customer on update.

#### 2.5.3 Delete Email

- The system MUST support deleting an email entry.
- If the deleted email was primary, the system SHOULD automatically promote the next email to primary.

#### 2.5.4 List Emails

- The system MUST support listing all emails for a customer.

### 2.6 Customer Categories

#### 2.6.1 Create Category

- The system MUST support creating customer categories with a name and optional description.
- The system MUST enforce unique category names.

#### 2.6.2 Update Category

- The system MUST support updating category name and description.
- The system MUST enforce unique category names on update.

#### 2.6.3 Delete Category

- The system MUST prevent deletion of a category that is assigned to one or more customers. This MUST return a 409 Conflict error with the count of affected customers.

#### 2.6.4 List Categories

- The system MUST support listing all categories.
- The system SHOULD support pagination.

---

## 3. Validation Rules

### 3.1 Customer

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | Name | Required. 1–200 characters. | `INVALID_CUSTOMER_NAME` |
| V2 | Code | Optional on create (auto-generated if omitted). 1–20 characters. Alphanumeric + hyphens. | `INVALID_CUSTOMER_CODE` |
| V3 | TaxId | Optional. 1–50 characters when provided. | `INVALID_TAX_ID` |
| V4 | CategoryId | Optional. Must reference an existing category when provided. | `INVALID_CATEGORY` |
| V5 | Code | Must be unique across all customers (including soft-deleted). | `DUPLICATE_CUSTOMER_CODE` |
| V6 | TaxId | Must be unique across active customers when provided. | `DUPLICATE_TAX_ID` |
| V7 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |

### 3.2 Customer Account

| # | Field | Rule | Error Code |
|---|---|---|---|
| V8 | CurrencyCode | Required. ISO 4217 3-letter code (e.g., USD, EUR, BGN). | `INVALID_CURRENCY_CODE` |
| V9 | Description | Optional. Max 500 characters. | `INVALID_ACCOUNT_DESCRIPTION` |
| V10 | CurrencyCode | Must be unique per customer (one account per currency). | `DUPLICATE_CURRENCY_ACCOUNT` |
| V11 | Balance | Read-only. Must not be settable via API. | `BALANCE_NOT_SETTABLE` |

### 3.3 Customer Address

| # | Field | Rule | Error Code |
|---|---|---|---|
| V12 | AddressType | Required. One of: `Billing`, `Shipping`, `Both`. | `INVALID_ADDRESS_TYPE` |
| V13 | StreetLine1 | Required. 1–200 characters. | `INVALID_STREET_LINE1` |
| V14 | StreetLine2 | Optional. Max 200 characters. | `INVALID_STREET_LINE2` |
| V15 | City | Required. 1–100 characters. | `INVALID_CITY` |
| V16 | StateProvince | Optional. Max 100 characters. | `INVALID_STATE_PROVINCE` |
| V17 | PostalCode | Required. 1–20 characters. | `INVALID_POSTAL_CODE` |
| V18 | CountryCode | Required. ISO 3166-1 alpha-2 (2-letter code, e.g., US, BG, DE). | `INVALID_COUNTRY_CODE` |

### 3.4 Customer Phone

| # | Field | Rule | Error Code |
|---|---|---|---|
| V19 | PhoneType | Required. One of: `Mobile`, `Landline`, `Fax`. | `INVALID_PHONE_TYPE` |
| V20 | PhoneNumber | Required. 5–20 characters. Digits, spaces, hyphens, parentheses, and leading `+` allowed. | `INVALID_PHONE_NUMBER` |
| V21 | Extension | Optional. Max 10 characters. Digits only. | `INVALID_EXTENSION` |

### 3.5 Customer Email

| # | Field | Rule | Error Code |
|---|---|---|---|
| V22 | EmailType | Required. One of: `General`, `Billing`, `Support`. | `INVALID_EMAIL_TYPE` |
| V23 | EmailAddress | Required. Valid email format (RFC 5322). Max 256 characters. | `INVALID_EMAIL_ADDRESS` |
| V24 | EmailAddress | Must be unique per customer. | `DUPLICATE_CUSTOMER_EMAIL` |

### 3.6 Customer Category

| # | Field | Rule | Error Code |
|---|---|---|---|
| V25 | Name | Required. 1–100 characters. | `INVALID_CATEGORY_NAME` |
| V26 | Description | Optional. Max 500 characters. | `INVALID_CATEGORY_DESCRIPTION` |
| V27 | Name | Must be unique across all categories. | `DUPLICATE_CATEGORY_NAME` |

### 3.7 Account Merge

| # | Field | Rule | Error Code |
|---|---|---|---|
| V28 | SourceAccountId | Required. Must exist and be active. | `INVALID_SOURCE_ACCOUNT` |
| V29 | TargetAccountId | Required. Must exist and be active. | `INVALID_TARGET_ACCOUNT` |
| V30 | SourceAccountId + TargetAccountId | Must not be the same account. | `MERGE_SELF_NOT_ALLOWED` |
| V31 | Source + Target | Must have the same currency code. | `MERGE_CURRENCY_MISMATCH` |
| V32 | Source + Target | Must belong to the same customer. | `MERGE_DIFFERENT_CUSTOMERS` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Customer not found (or soft-deleted) | 404 | `CUSTOMER_NOT_FOUND` | Customer not found. |
| E2 | Duplicate customer code | 409 | `DUPLICATE_CUSTOMER_CODE` | A customer with this code already exists. |
| E3 | Duplicate tax ID (among active customers) | 409 | `DUPLICATE_TAX_ID` | An active customer with this tax ID already exists. |
| E4 | Invalid category reference | 400 | `INVALID_CATEGORY` | The specified customer category does not exist. |
| E5 | Category not found | 404 | `CATEGORY_NOT_FOUND` | Customer category not found. |
| E6 | Delete category with assigned customers | 409 | `CATEGORY_IN_USE` | Cannot delete category — it is assigned to {count} customer(s). |
| E7 | Duplicate category name | 409 | `DUPLICATE_CATEGORY_NAME` | A category with this name already exists. |
| E8 | Account not found | 404 | `ACCOUNT_NOT_FOUND` | Customer account not found. |
| E9 | Duplicate currency account for customer | 409 | `DUPLICATE_CURRENCY_ACCOUNT` | This customer already has an account with currency {currency}. |
| E10 | Deactivate account with non-zero balance | 409 | `ACCOUNT_HAS_BALANCE` | Cannot deactivate account with a non-zero balance of {amount} {currency}. |
| E11 | Deactivate sole remaining account | 409 | `LAST_ACTIVE_ACCOUNT` | Cannot deactivate the last active account for this customer. |
| E12 | Merge accounts with different currencies | 400 | `MERGE_CURRENCY_MISMATCH` | Cannot merge accounts with different currency codes. |
| E13 | Merge account into itself | 400 | `MERGE_SELF_NOT_ALLOWED` | Cannot merge an account into itself. |
| E14 | Merge accounts from different customers | 400 | `MERGE_DIFFERENT_CUSTOMERS` | Cannot merge accounts belonging to different customers. |
| E15 | Merge into inactive target | 404 | `ACCOUNT_NOT_FOUND` | Customer account not found. |
| E16 | Address not found | 404 | `ADDRESS_NOT_FOUND` | Customer address not found. |
| E17 | Phone not found | 404 | `PHONE_NOT_FOUND` | Customer phone not found. |
| E18 | Email not found | 404 | `EMAIL_NOT_FOUND` | Customer email not found. |
| E19 | Duplicate email per customer | 409 | `DUPLICATE_CUSTOMER_EMAIL` | This customer already has this email address. |
| E20 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more fields are invalid. (ProblemDetails with field errors) |
| E21 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E22 | Deactivate customer with active accounts | 409 | `CUSTOMER_HAS_ACTIVE_ACCOUNTS` | Cannot deactivate customer — {count} account(s) have non-zero balances. |
| E23 | Reactivate customer with conflicting code | 409 | `DUPLICATE_CUSTOMER_CODE` | A customer with this code already exists. |
| E24 | Reactivate customer with conflicting tax ID | 409 | `DUPLICATE_TAX_ID` | An active customer with this tax ID already exists. |
| E25 | Unsupported currency code | 400 | `INVALID_CURRENCY_CODE` | The currency code {code} is not supported. |
| E26 | Attempt to set balance via API | 400 | `BALANCE_NOT_SETTABLE` | Account balance cannot be set directly. |

All error responses MUST use ProblemDetails (RFC 7807) format:

```json
{
  "type": "https://warehouse.local/errors/{error-code}",
  "title": "Short error title",
  "status": 400,
  "detail": "Human-readable description.",
  "instance": "/api/v1/customers/{id}",
  "errors": {}
}
```

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| **Customers** | | | | |
| POST | `/api/v1/customers` | Create customer | Yes | `customers:write` |
| GET | `/api/v1/customers` | List/search customers (paginated) | Yes | `customers:read` |
| GET | `/api/v1/customers/{id}` | Get customer by ID (with contacts, accounts, category) | Yes | `customers:read` |
| PUT | `/api/v1/customers/{id}` | Update customer | Yes | `customers:update` |
| DELETE | `/api/v1/customers/{id}` | Soft-delete (deactivate) customer | Yes | `customers:delete` |
| POST | `/api/v1/customers/{id}/reactivate` | Reactivate soft-deleted customer | Yes | `customers:update` |
| **Customer Accounts** | | | | |
| POST | `/api/v1/customers/{customerId}/accounts` | Create account | Yes | `customers:update` |
| GET | `/api/v1/customers/{customerId}/accounts` | List accounts for customer | Yes | `customers:read` |
| PUT | `/api/v1/customers/{customerId}/accounts/{accountId}` | Update account | Yes | `customers:update` |
| DELETE | `/api/v1/customers/{customerId}/accounts/{accountId}` | Deactivate account | Yes | `customers:update` |
| POST | `/api/v1/customers/{customerId}/accounts/merge` | Merge two accounts | Yes | `customers:update` |
| **Customer Addresses** | | | | |
| POST | `/api/v1/customers/{customerId}/addresses` | Create address | Yes | `customers:update` |
| GET | `/api/v1/customers/{customerId}/addresses` | List addresses | Yes | `customers:read` |
| PUT | `/api/v1/customers/{customerId}/addresses/{addressId}` | Update address | Yes | `customers:update` |
| DELETE | `/api/v1/customers/{customerId}/addresses/{addressId}` | Delete address | Yes | `customers:update` |
| **Customer Phones** | | | | |
| POST | `/api/v1/customers/{customerId}/phones` | Create phone | Yes | `customers:update` |
| GET | `/api/v1/customers/{customerId}/phones` | List phones | Yes | `customers:read` |
| PUT | `/api/v1/customers/{customerId}/phones/{phoneId}` | Update phone | Yes | `customers:update` |
| DELETE | `/api/v1/customers/{customerId}/phones/{phoneId}` | Delete phone | Yes | `customers:update` |
| **Customer Emails** | | | | |
| POST | `/api/v1/customers/{customerId}/emails` | Create email | Yes | `customers:update` |
| GET | `/api/v1/customers/{customerId}/emails` | List emails | Yes | `customers:read` |
| PUT | `/api/v1/customers/{customerId}/emails/{emailId}` | Update email | Yes | `customers:update` |
| DELETE | `/api/v1/customers/{customerId}/emails/{emailId}` | Delete email | Yes | `customers:update` |
| **Customer Categories** | | | | |
| POST | `/api/v1/customer-categories` | Create category | Yes | `customers:write` |
| GET | `/api/v1/customer-categories` | List categories | Yes | `customers:read` |
| GET | `/api/v1/customer-categories/{id}` | Get category by ID | Yes | `customers:read` |
| PUT | `/api/v1/customer-categories/{id}` | Update category | Yes | `customers:update` |
| DELETE | `/api/v1/customer-categories/{id}` | Delete category | Yes | `customers:delete` |

---

## 6. Database Schema

**Schema name:** `customers`

### Tables

**customers.Customers**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Code | NVARCHAR(20) | NOT NULL, UNIQUE |
| Name | NVARCHAR(200) | NOT NULL |
| TaxId | NVARCHAR(50) | NULL |
| CategoryId | INT | NULL, FK -> customers.CustomerCategories(Id) |
| Notes | NVARCHAR(2000) | NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| IsDeleted | BIT | NOT NULL, DEFAULT 0 |
| DeletedAtUtc | DATETIME2(7) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users — no EF navigation) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (cross-schema ref to auth.Users — no EF navigation) |

**customers.CustomerAccounts**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| CustomerId | INT | NOT NULL, FK -> customers.Customers(Id) |
| CurrencyCode | NVARCHAR(3) | NOT NULL |
| Balance | DECIMAL(18,4) | NOT NULL, DEFAULT 0 |
| Description | NVARCHAR(500) | NULL |
| IsPrimary | BIT | NOT NULL, DEFAULT 0 |
| IsDeleted | BIT | NOT NULL, DEFAULT 0 |
| DeletedAtUtc | DATETIME2(7) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

**customers.CustomerAddresses**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| CustomerId | INT | NOT NULL, FK -> customers.Customers(Id) |
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

**customers.CustomerPhones**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| CustomerId | INT | NOT NULL, FK -> customers.Customers(Id) |
| PhoneType | NVARCHAR(20) | NOT NULL |
| PhoneNumber | NVARCHAR(20) | NOT NULL |
| Extension | NVARCHAR(10) | NULL |
| IsPrimary | BIT | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**customers.CustomerEmails**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| CustomerId | INT | NOT NULL, FK -> customers.Customers(Id) |
| EmailType | NVARCHAR(20) | NOT NULL |
| EmailAddress | NVARCHAR(256) | NOT NULL |
| IsPrimary | BIT | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**customers.CustomerCategories**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Name | NVARCHAR(100) | NOT NULL, UNIQUE |
| Description | NVARCHAR(500) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_Customers_Code | customers.Customers | Code | Unique |
| IX_Customers_TaxId | customers.Customers | TaxId | Non-unique (filtered: IsDeleted = 0 AND TaxId IS NOT NULL) |
| IX_Customers_CategoryId | customers.Customers | CategoryId | Non-unique |
| IX_Customers_IsDeleted | customers.Customers | IsDeleted | Non-unique (filtered: IsDeleted = 0) |
| IX_Customers_Name | customers.Customers | Name | Non-unique |
| IX_CustomerAccounts_CustomerId | customers.CustomerAccounts | CustomerId | Non-unique |
| IX_CustomerAccounts_CustomerId_CurrencyCode | customers.CustomerAccounts | CustomerId, CurrencyCode | Unique (filtered: IsDeleted = 0) |
| IX_CustomerAddresses_CustomerId | customers.CustomerAddresses | CustomerId | Non-unique |
| IX_CustomerPhones_CustomerId | customers.CustomerPhones | CustomerId | Non-unique |
| IX_CustomerEmails_CustomerId | customers.CustomerEmails | CustomerId | Non-unique |
| IX_CustomerEmails_CustomerId_EmailAddress | customers.CustomerEmails | CustomerId, EmailAddress | Unique |
| IX_CustomerCategories_Name | customers.CustomerCategories | Name | Unique |

---

## 7. Versioning Notes

- **v1 — Initial specification (2026-04-04)**
  - Customer CRUD with soft delete and reactivation
  - Multi-currency customer accounts with merge capability
  - Contact information management (addresses, phones, emails)
  - Customer category management
  - ProblemDetails error responses
  - Full validation rule set
  - Database schema on `customers` schema

---

## 8. Test Plan

### Unit Tests — CustomerServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedCustomer` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `CreateAsync_DuplicateTaxId_ReturnsConflictError` [Unit]
- `CreateAsync_InvalidCategoryId_ReturnsValidationError` [Unit]
- `CreateAsync_NoCodeProvided_GeneratesCode` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedCustomer` [Unit]
- `UpdateAsync_SoftDeletedCustomer_ReturnsNotFound` [Unit]
- `UpdateAsync_DuplicateTaxId_ReturnsConflictError` [Unit]
- `GetByIdAsync_ExistingCustomer_ReturnsCustomerWithDetails` [Unit]
- `GetByIdAsync_SoftDeletedCustomer_ReturnsNotFound` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `SearchAsync_DefaultSort_SortsByNameAscending` [Unit]
- `DeactivateAsync_ActiveCustomer_SetsIsDeletedAndDeletedAt` [Unit]
- `DeactivateAsync_CustomerWithActiveAccounts_ReturnsConflict` [Unit]
- `DeactivateAsync_AlreadyDeleted_ReturnsNotFound` [Unit]
- `ReactivateAsync_SoftDeletedCustomer_ClearsDeletedFlags` [Unit]
- `ReactivateAsync_ConflictingCode_ReturnsConflictError` [Unit]
- `ReactivateAsync_ConflictingTaxId_ReturnsConflictError` [Unit]

### Unit Tests — CustomerAccountServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedAccount` [Unit]
- `CreateAsync_DuplicateCurrency_ReturnsConflictError` [Unit]
- `CreateAsync_NonExistentCustomer_ReturnsNotFound` [Unit]
- `CreateAsync_FirstAccount_SetsIsPrimaryTrue` [Unit]
- `CreateAsync_InvalidCurrencyCode_ReturnsValidationError` [Unit]
- `UpdateAsync_SetPrimary_UnsetsOtherPrimary` [Unit]
- `DeactivateAsync_NonZeroBalance_ReturnsConflict` [Unit]
- `DeactivateAsync_LastActiveAccount_ReturnsConflict` [Unit]
- `MergeAsync_ValidRequest_TransfersBalanceAndDeletesSource` [Unit]
- `MergeAsync_DifferentCurrencies_ReturnsValidationError` [Unit]
- `MergeAsync_SameAccount_ReturnsValidationError` [Unit]
- `MergeAsync_DifferentCustomers_ReturnsValidationError` [Unit]
- `MergeAsync_InactiveTarget_ReturnsNotFound` [Unit]

### Unit Tests — CustomerContactServiceTests

- `CreateAddress_ValidRequest_ReturnsCreatedAddress` [Unit]
- `CreateAddress_FirstOfType_SetsDefaultTrue` [Unit]
- `DeleteAddress_DefaultAddress_PromotesNext` [Unit]
- `CreatePhone_FirstPhone_SetsPrimaryTrue` [Unit]
- `UpdatePhone_SetPrimary_UnsetsOtherPrimary` [Unit]
- `DeletePhone_PrimaryPhone_PromotesNext` [Unit]
- `CreateEmail_ValidRequest_ReturnsCreatedEmail` [Unit]
- `CreateEmail_DuplicateForCustomer_ReturnsConflict` [Unit]
- `CreateEmail_FirstEmail_SetsPrimaryTrue` [Unit]
- `UpdateEmail_DuplicateForCustomer_ReturnsConflict` [Unit]
- `DeleteEmail_PrimaryEmail_PromotesNext` [Unit]

### Unit Tests — CustomerCategoryServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedCategory` [Unit]
- `CreateAsync_DuplicateName_ReturnsConflictError` [Unit]
- `UpdateAsync_DuplicateName_ReturnsConflictError` [Unit]
- `DeleteAsync_CategoryWithCustomers_ReturnsConflict` [Unit]
- `DeleteAsync_UnusedCategory_DeletesSuccessfully` [Unit]

### Unit Tests — Validation

- `CreateCustomerRequestValidator_MissingName_Fails` [Unit]
- `CreateCustomerRequestValidator_NameTooLong_Fails` [Unit]
- `CreateCustomerRequestValidator_CodeTooLong_Fails` [Unit]
- `CreateCustomerRequestValidator_InvalidCodeCharacters_Fails` [Unit]
- `CreateAccountRequestValidator_MissingCurrencyCode_Fails` [Unit]
- `CreateAccountRequestValidator_InvalidCurrencyCode_Fails` [Unit]
- `CreateAddressRequestValidator_MissingRequiredFields_Fails` [Unit]
- `CreateAddressRequestValidator_InvalidAddressType_Fails` [Unit]
- `CreateAddressRequestValidator_InvalidCountryCode_Fails` [Unit]
- `CreatePhoneRequestValidator_InvalidPhoneNumber_Fails` [Unit]
- `CreatePhoneRequestValidator_InvalidPhoneType_Fails` [Unit]
- `CreateEmailRequestValidator_InvalidEmailFormat_Fails` [Unit]
- `CreateEmailRequestValidator_InvalidEmailType_Fails` [Unit]
- `MergeAccountsRequestValidator_MissingSourceOrTarget_Fails` [Unit]
- `CreateCategoryRequestValidator_MissingName_Fails` [Unit]
- `CreateCategoryRequestValidator_NameTooLong_Fails` [Unit]

### Integration Tests — CustomersControllerTests

- `CreateCustomer_ValidPayload_Returns201` [Integration]
- `CreateCustomer_DuplicateCode_Returns409` [Integration]
- `CreateCustomer_DuplicateTaxId_Returns409` [Integration]
- `CreateCustomer_InvalidPayload_Returns400ProblemDetails` [Integration]
- `CreateCustomer_Unauthenticated_Returns401` [Integration]
- `CreateCustomer_InsufficientPermissions_Returns403` [Integration]
- `GetCustomer_ExistingId_Returns200WithDetails` [Integration]
- `GetCustomer_SoftDeletedId_Returns404` [Integration]
- `GetCustomer_NonExistentId_Returns404` [Integration]
- `UpdateCustomer_ValidPayload_Returns200` [Integration]
- `UpdateCustomer_SoftDeletedCustomer_Returns404` [Integration]
- `DeleteCustomer_ActiveCustomer_Returns204` [Integration]
- `DeleteCustomer_AlreadyDeleted_Returns404` [Integration]
- `SearchCustomers_WithNameFilter_ReturnsMatchingResults` [Integration]
- `SearchCustomers_Pagination_ReturnsCorrectPage` [Integration]
- `ReactivateCustomer_SoftDeleted_Returns200` [Integration]

### Integration Tests — CustomerAccountsControllerTests

- `CreateAccount_ValidPayload_Returns201` [Integration]
- `CreateAccount_DuplicateCurrency_Returns409` [Integration]
- `CreateAccount_NonExistentCustomer_Returns404` [Integration]
- `ListAccounts_ReturnsAllAccountsForCustomer` [Integration]
- `UpdateAccount_SetPrimary_Returns200` [Integration]
- `DeactivateAccount_NonZeroBalance_Returns409` [Integration]
- `DeactivateAccount_LastAccount_Returns409` [Integration]
- `MergeAccounts_ValidRequest_Returns200AndTransfersBalance` [Integration]
- `MergeAccounts_DifferentCurrencies_Returns400` [Integration]
- `MergeAccounts_SameAccount_Returns400` [Integration]

### Integration Tests — CustomerContactsControllerTests

- `CreateAddress_ValidPayload_Returns201` [Integration]
- `ListAddresses_ReturnsAllForCustomer` [Integration]
- `UpdateAddress_ValidPayload_Returns200` [Integration]
- `DeleteAddress_Returns204` [Integration]
- `CreatePhone_ValidPayload_Returns201` [Integration]
- `ListPhones_ReturnsAllForCustomer` [Integration]
- `DeletePhone_Returns204` [Integration]
- `CreateEmail_ValidPayload_Returns201` [Integration]
- `CreateEmail_DuplicateForCustomer_Returns409` [Integration]
- `ListEmails_ReturnsAllForCustomer` [Integration]
- `DeleteEmail_Returns204` [Integration]

### Integration Tests — CustomerCategoriesControllerTests

- `CreateCategory_ValidPayload_Returns201` [Integration]
- `CreateCategory_DuplicateName_Returns409` [Integration]
- `ListCategories_Returns200` [Integration]
- `UpdateCategory_ValidPayload_Returns200` [Integration]
- `DeleteCategory_WithCustomers_Returns409` [Integration]
- `DeleteCategory_Unused_Returns204` [Integration]

---

## 9. Acceptance Criteria

- [ ] Customers can be created, updated, searched, deactivated, and reactivated
- [ ] Customer codes are auto-generated when not provided and are always unique
- [ ] Tax IDs are unique among active customers
- [ ] Customer categories can be managed and assigned to customers
- [ ] Multi-currency accounts can be created per customer (one per currency)
- [ ] Account merge transfers balance within the same currency and same customer
- [ ] Accounts with non-zero balance cannot be deactivated
- [ ] Contact information (addresses, phones, emails) can be managed per customer
- [ ] Primary/default promotion occurs automatically when the primary/default item is removed
- [ ] Soft-deleted customers are invisible to standard queries
- [ ] All endpoints require JWT authentication and permission-based authorization (SDD-AUTH-001)
- [ ] All error responses use ProblemDetails (RFC 7807) format
- [ ] All validation rules are enforced with appropriate error codes
- [ ] Database uses `customers` schema with proper FK references to `auth.Users`

---

## Key Files

- `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomersController.cs`
- `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomerAccountsController.cs`
- `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomerContactsController.cs`
- `src/Interfaces/Customers/Warehouse.Customers.API/Controllers/CustomerCategoriesController.cs`
- `src/Databases/Warehouse.Customers.DBModel/Models/Customer.cs`
- `src/Databases/Warehouse.Customers.DBModel/Models/CustomerAccount.cs`
- `src/Databases/Warehouse.Customers.DBModel/Models/CustomerAddress.cs`
- `src/Databases/Warehouse.Customers.DBModel/Models/CustomerPhone.cs`
- `src/Databases/Warehouse.Customers.DBModel/Models/CustomerEmail.cs`
- `src/Databases/Warehouse.Customers.DBModel/Models/CustomerCategory.cs`
- `src/Databases/Warehouse.Customers.DBModel/CustomersDbContext.cs`
- `src/Warehouse.ServiceModel/DTOs/Customers/`
- `src/Warehouse.ServiceModel/Requests/Customers/`
- `src/Warehouse.Mapping/Profiles/Customers/CustomerMappingProfile.cs`
- `src/Interfaces/Customers/Warehouse.Customers.API.Tests/`
