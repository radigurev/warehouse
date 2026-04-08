# SDD-INV-001 — Products and Catalog

> Status: Active
> Last updated: 2026-04-08
> Owner: TBD
> Category: Core

## 1. Context & Scope

This spec defines the Product catalog sub-domain for the Warehouse Inventory system. It covers product lifecycle management, product categories, units of measure, bill of materials (BOM), product accessories, and product substitutes.

**In scope:**
- Product CRUD with soft-delete and reactivation
- Product search and filtering with pagination
- Product category management (hierarchical)
- Unit of measure management
- Bill of Materials (BOM) with parent/child product relationships
- Product accessories (related product lookups)
- Product substitutes (interchangeable product lookups)
- Audit columns on all entities (CreatedAtUtc, ModifiedAtUtc, CreatedByUserId, ModifiedByUserId)

**Out of scope:**
- Product pricing and discount rules (covered by Finance service)
- Product images and media (future enhancement)
- Product import/export (future enhancement)
- Stock levels and movements (see SDD-INV-002)
- Warehouse structure (see SDD-INV-003)

**Related specs:**
- `SDD-AUTH-001` — All endpoints require JWT authentication and permission-based authorization.
- `SDD-INV-002` — Stock levels reference products defined here.
- `SDD-INV-003` — Warehouse locations reference products for stock placement.
- `SDD-INV-004` — Stocktaking references products for count verification.

**ISA-95 Conformance:** Conforms to ISA-95 Part 2, Section 7 — Material Model (Material Definition, Material Class, Unit of Measure, Bill of Material, Alternate/Related Material).

---

## 2. Behavior

### 2.1 Product Management

#### 2.1.1 Create Product

- The system MUST support creating a product with a code, name, description, category, unit of measure, `RequiresBatchTracking` flag, and active status.
- The system MUST default `RequiresBatchTracking` to `false` when not provided.
- The system MUST enforce unique product codes across all products (including soft-deleted).
- The system MUST set `IsActive = true` by default on creation.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.
- The system MUST return the created product with its generated ID.

**Edge cases:**
- Creating a product with a duplicate code MUST return a 409 Conflict error.
- Creating a product with an invalid (non-existent) category ID MUST return a 400 Validation error.
- Creating a product with an invalid (non-existent) unit of measure ID MUST return a 400 Validation error.

#### 2.1.2 Update Product

- The system MUST support updating product fields: name, description, category, unit of measure, notes, and `RequiresBatchTracking`.
- The system MUST NOT allow changing the product code after creation.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on every update.
- Updating a soft-deleted product MUST return a 404 Not Found error.

**Edge cases:**
- Updating with a non-existent category ID MUST return a 400 Validation error.
- Updating with a non-existent unit of measure ID MUST return a 400 Validation error.

#### 2.1.3 Get Product

- The system MUST support retrieving a single product by ID, including category name, unit of measure name, and `RequiresBatchTracking` flag.
- Retrieving a soft-deleted product MUST return a 404 Not Found error.

#### 2.1.4 Search Products

- The system MUST support paginated listing of products with configurable page size and page number.
- The system MUST support filtering by: name (contains), code (exact or starts-with), category ID, and active status.
- The system MUST support sorting by name, code, and creation date.
- The system MUST default to sorting by name ascending when no sort is specified.
- The system MUST exclude soft-deleted products from search results by default.
- The system SHOULD support a query parameter to include soft-deleted products for administrative purposes.

#### 2.1.5 Deactivate Product (Soft Delete)

- The system MUST support soft-deleting a product by setting `IsDeleted = true` and `DeletedAtUtc = current UTC time`.
- Soft-deleted products MUST NOT appear in standard queries or searches.
- The system MUST set `IsActive = false` when a product is soft-deleted.
- The system SHOULD prevent deactivation if the product has non-zero stock levels. This check SHOULD return a 409 Conflict error.

**Edge cases:**
- Deactivating an already soft-deleted product MUST return a 404 Not Found error.

#### 2.1.6 Reactivate Product

- The system MUST support reactivating a soft-deleted product by setting `IsDeleted = false`, `DeletedAtUtc = null`, and `IsActive = true`.
- The system MUST update `ModifiedAtUtc` and `ModifiedByUserId` on reactivation.
- Reactivating a product that is already active (not soft-deleted) MUST return a 409 Conflict error with code `PRODUCT_ALREADY_ACTIVE`.
- Reactivating a product whose code now conflicts with another active product MUST return a 409 Conflict error.

### 2.2 Product Categories

#### 2.2.1 Create Category

- The system MUST support creating product categories with a name, optional description, and optional parent category ID (for hierarchical categories).
- The system MUST enforce unique category names.

#### 2.2.2 Update Category

- The system MUST support updating category name, description, and parent category.
- The system MUST enforce unique category names on update.
- The system MUST prevent setting a category as its own parent. This MUST return a 400 Validation error.

#### 2.2.3 Delete Category

- The system MUST prevent deletion of a category that is assigned to one or more products. This MUST return a 409 Conflict error with the count of affected products.
- The system MUST prevent deletion of a category that has child categories. This MUST return a 409 Conflict error.

#### 2.2.4 List Categories

- The system MUST support listing all categories.
- The system SHOULD support pagination.
- The system SHOULD return parent category information for hierarchical display.

### 2.3 Units of Measure

#### 2.3.1 Create Unit of Measure

- The system MUST support creating units of measure with a code, name, and optional description.
- The system MUST enforce unique unit codes.

#### 2.3.2 Update Unit of Measure

- The system MUST support updating unit name and description.
- The system MUST NOT allow changing the unit code after creation.

#### 2.3.3 Delete Unit of Measure

- The system MUST prevent deletion of a unit that is assigned to one or more products. This MUST return a 409 Conflict error.

#### 2.3.4 List Units of Measure

- The system MUST support listing all units of measure.

### 2.4 Bill of Materials (BOM)

#### 2.4.1 Create BOM

- The system MUST support creating a BOM header for a parent product with an optional name, version, and effective date.
- The system MUST enforce that a product has at most one active BOM at a time. Creating a BOM when one already exists MUST return a 409 Conflict error with code `BOM_ALREADY_EXISTS`.
- The system MUST record `CreatedAtUtc` and `CreatedByUserId` on creation.

#### 2.4.2 Add BOM Line

- The system MUST support adding component lines to a BOM with a child product ID, quantity, and optional notes.
- The system MUST prevent adding a product as its own component (self-referencing). This MUST return a 400 Validation error.
- The system MUST prevent duplicate child product entries within the same BOM. This MUST return a 409 Conflict error.

#### 2.4.3 Update BOM Line

- The system MUST support updating BOM line quantity and notes.

#### 2.4.4 Remove BOM Line

- The system MUST support removing a component line from a BOM.

#### 2.4.5 Get BOM

- The system MUST support retrieving a BOM by parent product ID, including all component lines with product details.

#### 2.4.6 Delete BOM

- The system MUST support deleting an entire BOM header and all its lines.

### 2.5 Product Accessories

#### 2.5.1 Add Accessory

- The system MUST support linking a product to an accessory product.
- The system MUST prevent adding a product as its own accessory. This MUST return a 400 Validation error.
- The system MUST prevent duplicate accessory links. This MUST return a 409 Conflict error.

#### 2.5.2 Remove Accessory

- The system MUST support removing an accessory link.

#### 2.5.3 List Accessories

- The system MUST support listing all accessories for a product.

### 2.6 Product Substitutes

#### 2.6.1 Add Substitute

- The system MUST support linking a product to a substitute product.
- The system MUST prevent adding a product as its own substitute. This MUST return a 400 Validation error.
- The system MUST prevent duplicate substitute links. This MUST return a 409 Conflict error.

#### 2.6.2 Remove Substitute

- The system MUST support removing a substitute link.

#### 2.6.3 List Substitutes

- The system MUST support listing all substitutes for a product.

---

## 3. Validation Rules

### 3.1 Product

| # | Field | Rule | Error Code |
|---|---|---|---|
| V1 | Code | Required. 1-50 characters. Alphanumeric, hyphens, underscores. | `INVALID_PRODUCT_CODE` |
| V2 | Name | Required. 1-200 characters. | `INVALID_PRODUCT_NAME` |
| V3 | Description | Optional. Max 2000 characters. | `INVALID_PRODUCT_DESCRIPTION` |
| V4 | CategoryId | Optional. Must reference an existing category when provided. | `INVALID_CATEGORY` |
| V5 | UnitOfMeasureId | Required. Must reference an existing unit. | `INVALID_UNIT_OF_MEASURE` |
| V6 | Code | Must be unique across all products (including soft-deleted). | `DUPLICATE_PRODUCT_CODE` |
| V7 | Notes | Optional. Max 2000 characters. | `INVALID_NOTES` |
| V8 | Sku | Optional. 1-100 characters when provided. | `INVALID_SKU` |
| V9 | Barcode | Optional. 1-100 characters when provided. | `INVALID_BARCODE` |
| V10 | RequiresBatchTracking | Optional. Boolean. Defaults to `false`. When `true`, stock movements for this product MUST include a BatchId (enforced by SDD-INV-002). | — |

### 3.2 Product Category

| # | Field | Rule | Error Code |
|---|---|---|---|
| V10 | Name | Required. 1-100 characters. | `INVALID_CATEGORY_NAME` |
| V11 | Description | Optional. Max 500 characters. | `INVALID_CATEGORY_DESCRIPTION` |
| V12 | Name | Must be unique across all categories. | `DUPLICATE_CATEGORY_NAME` |
| V13 | ParentCategoryId | Optional. Must reference an existing category. Must not be self. | `INVALID_PARENT_CATEGORY` |

### 3.3 Unit of Measure

| # | Field | Rule | Error Code |
|---|---|---|---|
| V14 | Code | Required. 1-10 characters. Uppercase alphanumeric. | `INVALID_UNIT_CODE` |
| V15 | Name | Required. 1-50 characters. | `INVALID_UNIT_NAME` |
| V16 | Description | Optional. Max 200 characters. | `INVALID_UNIT_DESCRIPTION` |
| V17 | Code | Must be unique across all units. | `DUPLICATE_UNIT_CODE` |

### 3.4 Bill of Materials

| # | Field | Rule | Error Code |
|---|---|---|---|
| V18 | ParentProductId | Required. Must reference an existing product. | `INVALID_PARENT_PRODUCT` |
| V19 | Name | Optional. Max 100 characters. | `INVALID_BOM_NAME` |
| V20 | ChildProductId | Required. Must reference an existing product. Must not equal parent. | `INVALID_CHILD_PRODUCT` |
| V21 | Quantity | Required. Must be > 0. | `INVALID_BOM_QUANTITY` |
| V22 | ChildProductId | Must be unique within the same BOM. | `DUPLICATE_BOM_LINE` |

### 3.5 Product Accessories & Substitutes

| # | Field | Rule | Error Code |
|---|---|---|---|
| V23 | AccessoryProductId | Required. Must reference an existing product. Must not equal source product. | `INVALID_ACCESSORY_PRODUCT` |
| V24 | AccessoryProductId | Must be unique per source product. | `DUPLICATE_ACCESSORY` |
| V25 | SubstituteProductId | Required. Must reference an existing product. Must not equal source product. | `INVALID_SUBSTITUTE_PRODUCT` |
| V26 | SubstituteProductId | Must be unique per source product. | `DUPLICATE_SUBSTITUTE` |

---

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | Product not found (or soft-deleted) | 404 | `PRODUCT_NOT_FOUND` | Product not found. |
| E2 | Duplicate product code | 409 | `DUPLICATE_PRODUCT_CODE` | A product with this code already exists. |
| E3 | Invalid category reference | 400 | `INVALID_CATEGORY` | The specified product category does not exist. |
| E4 | Invalid unit of measure reference | 400 | `INVALID_UNIT_OF_MEASURE` | The specified unit of measure does not exist. |
| E5 | Category not found | 404 | `CATEGORY_NOT_FOUND` | Product category not found. |
| E6 | Delete category with products | 409 | `CATEGORY_IN_USE` | Cannot delete category -- it is assigned to {count} product(s). |
| E7 | Delete category with children | 409 | `CATEGORY_HAS_CHILDREN` | Cannot delete category -- it has {count} child category(ies). |
| E8 | Duplicate category name | 409 | `DUPLICATE_CATEGORY_NAME` | A category with this name already exists. |
| E9 | Unit not found | 404 | `UNIT_NOT_FOUND` | Unit of measure not found. |
| E10 | Duplicate unit code | 409 | `DUPLICATE_UNIT_CODE` | A unit of measure with this code already exists. |
| E11 | Delete unit with products | 409 | `UNIT_IN_USE` | Cannot delete unit -- it is assigned to {count} product(s). |
| E12 | BOM not found | 404 | `BOM_NOT_FOUND` | Bill of materials not found. |
| E13 | BOM line not found | 404 | `BOM_LINE_NOT_FOUND` | BOM line not found. |
| E14 | Self-referencing BOM | 400 | `BOM_SELF_REFERENCE` | A product cannot be a component of itself. |
| E15 | Duplicate BOM line | 409 | `DUPLICATE_BOM_LINE` | This component product is already in the BOM. |
| E16 | Self-referencing accessory | 400 | `ACCESSORY_SELF_REFERENCE` | A product cannot be an accessory of itself. |
| E17 | Duplicate accessory | 409 | `DUPLICATE_ACCESSORY` | This accessory link already exists. |
| E18 | Accessory not found | 404 | `ACCESSORY_NOT_FOUND` | Product accessory link not found. |
| E19 | Self-referencing substitute | 400 | `SUBSTITUTE_SELF_REFERENCE` | A product cannot be a substitute of itself. |
| E20 | Duplicate substitute | 409 | `DUPLICATE_SUBSTITUTE` | This substitute link already exists. |
| E21 | Substitute not found | 404 | `SUBSTITUTE_NOT_FOUND` | Product substitute link not found. |
| E22 | Category self-parent | 400 | `CATEGORY_SELF_PARENT` | A category cannot be its own parent. |
| E23 | Deactivate product with stock | 409 | `PRODUCT_HAS_STOCK` | Cannot deactivate product -- it has non-zero stock levels. |
| E24 | Validation failure (field-level) | 400 | `VALIDATION_ERROR` | One or more fields are invalid. (ProblemDetails with field errors) |
| E25 | Insufficient permissions | 403 | `FORBIDDEN` | You do not have permission to perform this action. |
| E26 | Reactivate already active product | 409 | `PRODUCT_ALREADY_ACTIVE` | Product is already active. |
| E27 | Create BOM when one already exists for product | 409 | `BOM_ALREADY_EXISTS` | An active bill of materials already exists for this product. |

All error responses MUST use ProblemDetails (RFC 7807) format.

---

## 5. API Endpoints

| Method | Route | Description | Auth Required | Permission |
|---|---|---|---|---|
| **Products** | | | | |
| POST | `/api/v1/products` | Create product | Yes | `products:create` |
| GET | `/api/v1/products` | List/search products (paginated) | Yes | `products:read` |
| GET | `/api/v1/products/{id}` | Get product by ID | Yes | `products:read` |
| PUT | `/api/v1/products/{id}` | Update product | Yes | `products:update` |
| DELETE | `/api/v1/products/{id}` | Soft-delete product | Yes | `products:delete` |
| POST | `/api/v1/products/{id}/reactivate` | Reactivate soft-deleted product | Yes | `products:update` |
| **Product Categories** | | | | |
| POST | `/api/v1/product-categories` | Create category | Yes | `product-categories:create` |
| GET | `/api/v1/product-categories` | List categories | Yes | `product-categories:read` |
| GET | `/api/v1/product-categories/{id}` | Get category by ID | Yes | `product-categories:read` |
| PUT | `/api/v1/product-categories/{id}` | Update category | Yes | `product-categories:update` |
| DELETE | `/api/v1/product-categories/{id}` | Delete category | Yes | `product-categories:delete` |
| **Units of Measure** | | | | |
| POST | `/api/v1/units-of-measure` | Create unit | Yes | `units-of-measure:create` |
| GET | `/api/v1/units-of-measure` | List units | Yes | `units-of-measure:read` |
| GET | `/api/v1/units-of-measure/{id}` | Get unit by ID | Yes | `units-of-measure:read` |
| PUT | `/api/v1/units-of-measure/{id}` | Update unit | Yes | `units-of-measure:update` |
| DELETE | `/api/v1/units-of-measure/{id}` | Delete unit | Yes | `units-of-measure:delete` |
| **Bill of Materials** | | | | |
| POST | `/api/v1/products/{productId}/bom` | Create BOM for product | Yes | `bom:create` |
| GET | `/api/v1/products/{productId}/bom` | Get BOM for product | Yes | `bom:read` |
| DELETE | `/api/v1/products/{productId}/bom` | Delete BOM | Yes | `bom:delete` |
| POST | `/api/v1/products/{productId}/bom/lines` | Add BOM line | Yes | `bom:update` |
| PUT | `/api/v1/products/{productId}/bom/lines/{lineId}` | Update BOM line | Yes | `bom:update` |
| DELETE | `/api/v1/products/{productId}/bom/lines/{lineId}` | Remove BOM line | Yes | `bom:delete` |
| **Product Accessories** | | | | |
| POST | `/api/v1/products/{productId}/accessories` | Add accessory | Yes | `products:update` |
| GET | `/api/v1/products/{productId}/accessories` | List accessories | Yes | `products:read` |
| DELETE | `/api/v1/products/{productId}/accessories/{accessoryId}` | Remove accessory | Yes | `products:update` |
| **Product Substitutes** | | | | |
| POST | `/api/v1/products/{productId}/substitutes` | Add substitute | Yes | `products:update` |
| GET | `/api/v1/products/{productId}/substitutes` | List substitutes | Yes | `products:read` |
| DELETE | `/api/v1/products/{productId}/substitutes/{substituteId}` | Remove substitute | Yes | `products:update` |

---

## 6. Database Schema

**Schema name:** `inventory`

### Tables

**inventory.Products**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Code | NVARCHAR(50) | NOT NULL, UNIQUE |
| Name | NVARCHAR(200) | NOT NULL |
| Description | NVARCHAR(2000) | NULL |
| Sku | NVARCHAR(100) | NULL |
| Barcode | NVARCHAR(100) | NULL |
| CategoryId | INT | NULL, FK -> inventory.ProductCategories(Id) |
| UnitOfMeasureId | INT | NOT NULL, FK -> inventory.UnitsOfMeasure(Id) |
| Notes | NVARCHAR(2000) | NULL |
| RequiresBatchTracking | BIT | NOT NULL, DEFAULT 0 |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| IsDeleted | BIT | NOT NULL, DEFAULT 0 |
| DeletedAtUtc | DATETIME2(7) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**inventory.ProductCategories**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Name | NVARCHAR(100) | NOT NULL, UNIQUE |
| Description | NVARCHAR(500) | NULL |
| ParentCategoryId | INT | NULL, FK -> inventory.ProductCategories(Id) (self-referencing) |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**inventory.UnitsOfMeasure**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| Code | NVARCHAR(10) | NOT NULL, UNIQUE |
| Name | NVARCHAR(50) | NOT NULL |
| Description | NVARCHAR(200) | NULL |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| ModifiedAtUtc | DATETIME2(7) | NULL |

**inventory.BillOfMaterials**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ParentProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| Name | NVARCHAR(100) | NULL |
| Version | NVARCHAR(20) | NULL |
| IsActive | BIT | NOT NULL, DEFAULT 1 |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |
| CreatedByUserId | INT | NOT NULL (cross-schema ref to auth.Users) |
| ModifiedAtUtc | DATETIME2(7) | NULL |
| ModifiedByUserId | INT | NULL (cross-schema ref to auth.Users) |

**inventory.BomLines**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| BillOfMaterialsId | INT | NOT NULL, FK -> inventory.BillOfMaterials(Id) |
| ChildProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| Quantity | DECIMAL(18,4) | NOT NULL |
| Notes | NVARCHAR(500) | NULL |

**inventory.ProductAccessories**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| AccessoryProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

**inventory.ProductSubstitutes**

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, IDENTITY(1,1) |
| ProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| SubstituteProductId | INT | NOT NULL, FK -> inventory.Products(Id) |
| CreatedAtUtc | DATETIME2(7) | NOT NULL, DEFAULT SYSUTCDATETIME() |

### Indexes

| Name | Table | Columns | Type |
|---|---|---|---|
| IX_Products_Code | Products | Code | Unique |
| IX_Products_CategoryId | Products | CategoryId | Non-unique |
| IX_Products_UnitOfMeasureId | Products | UnitOfMeasureId | Non-unique |
| IX_Products_IsDeleted | Products | IsDeleted | Non-unique (filtered: IsDeleted = 0) |
| IX_Products_Name | Products | Name | Non-unique |
| IX_Products_Sku | Products | Sku | Non-unique (filtered: Sku IS NOT NULL) |
| IX_Products_Barcode | Products | Barcode | Non-unique (filtered: Barcode IS NOT NULL) |
| IX_ProductCategories_Name | ProductCategories | Name | Unique |
| IX_ProductCategories_ParentCategoryId | ProductCategories | ParentCategoryId | Non-unique |
| IX_UnitsOfMeasure_Code | UnitsOfMeasure | Code | Unique |
| IX_BillOfMaterials_ParentProductId | BillOfMaterials | ParentProductId | Non-unique |
| IX_BomLines_BillOfMaterialsId | BomLines | BillOfMaterialsId | Non-unique |
| IX_BomLines_ChildProductId | BomLines | ChildProductId | Non-unique |
| IX_BomLines_BomId_ChildProductId | BomLines | BillOfMaterialsId, ChildProductId | Unique |
| IX_ProductAccessories_ProductId | ProductAccessories | ProductId | Non-unique |
| IX_ProductAccessories_ProductId_AccessoryProductId | ProductAccessories | ProductId, AccessoryProductId | Unique |
| IX_ProductSubstitutes_ProductId | ProductSubstitutes | ProductId | Non-unique |
| IX_ProductSubstitutes_ProductId_SubstituteProductId | ProductSubstitutes | ProductId, SubstituteProductId | Unique |

---

## 7. Versioning Notes

- **v1 -- Initial specification (2026-04-05)**
  - Product CRUD with soft delete and reactivation
  - Product categories with hierarchical support
  - Units of measure management
  - Bill of Materials with component lines
  - Product accessories and substitutes
  - ProblemDetails error responses
  - Full validation rule set
  - Database schema on `inventory` schema

- **v2 -- Error alignment (2026-04-05)** (non-breaking)
  - Added `PRODUCT_ALREADY_ACTIVE` (409) error for reactivating an already active product
  - Added `BOM_ALREADY_EXISTS` (409) error for creating a BOM when one already exists
  - Added explicit edge case for BOM already exists on create
  - Added explicit edge case for reactivating an already active product
  - Status changed from Draft to Active

- **v3 — Batch tracking documentation (2026-04-08)** (non-breaking)
  - Documented `RequiresBatchTracking` field in Create/Update/Get Product behavior
  - Added V10 validation rule for `RequiresBatchTracking`
  - Added `RequiresBatchTracking` column to Products database schema
  - Cross-reference to SDD-INV-002 for batch enforcement on stock movements

---

## 8. Test Plan

### Unit Tests -- ProductServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedProduct` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `CreateAsync_InvalidCategoryId_ReturnsValidationError` [Unit]
- `CreateAsync_InvalidUnitOfMeasureId_ReturnsValidationError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedProduct` [Unit]
- `UpdateAsync_SoftDeletedProduct_ReturnsNotFound` [Unit]
- `GetByIdAsync_ExistingProduct_ReturnsProduct` [Unit]
- `GetByIdAsync_SoftDeletedProduct_ReturnsNotFound` [Unit]
- `SearchAsync_WithFilters_ReturnsFilteredResults` [Unit]
- `SearchAsync_DefaultSort_SortsByNameAscending` [Unit]
- `DeactivateAsync_ActiveProduct_SetsIsDeletedAndDeletedAt` [Unit]
- `DeactivateAsync_AlreadyDeleted_ReturnsNotFound` [Unit]
- `ReactivateAsync_SoftDeletedProduct_ClearsDeletedFlags` [Unit]
- `ReactivateAsync_AlreadyActive_ReturnsConflictError` [Unit]
- `ReactivateAsync_ConflictingCode_ReturnsConflictError` [Unit]

### Unit Tests -- ProductCategoryServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedCategory` [Unit]
- `CreateAsync_DuplicateName_ReturnsConflictError` [Unit]
- `UpdateAsync_DuplicateName_ReturnsConflictError` [Unit]
- `UpdateAsync_SelfParent_ReturnsValidationError` [Unit]
- `DeleteAsync_CategoryWithProducts_ReturnsConflict` [Unit]
- `DeleteAsync_CategoryWithChildren_ReturnsConflict` [Unit]
- `DeleteAsync_UnusedCategory_DeletesSuccessfully` [Unit]

### Unit Tests -- UnitOfMeasureServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedUnit` [Unit]
- `CreateAsync_DuplicateCode_ReturnsConflictError` [Unit]
- `UpdateAsync_ValidRequest_ReturnsUpdatedUnit` [Unit]
- `DeleteAsync_UnitWithProducts_ReturnsConflict` [Unit]
- `DeleteAsync_UnusedUnit_DeletesSuccessfully` [Unit]

### Unit Tests -- BomServiceTests

- `CreateAsync_ValidRequest_ReturnsCreatedBom` [Unit]
- `CreateAsync_BomAlreadyExists_ReturnsConflictError` [Unit]
- `AddLineAsync_ValidRequest_ReturnsCreatedLine` [Unit]
- `AddLineAsync_SelfReference_ReturnsValidationError` [Unit]
- `AddLineAsync_DuplicateChild_ReturnsConflictError` [Unit]
- `UpdateLineAsync_ValidRequest_ReturnsUpdatedLine` [Unit]
- `RemoveLineAsync_ExistingLine_RemovesSuccessfully` [Unit]
- `GetByProductIdAsync_ExistingBom_ReturnsBomWithLines` [Unit]
- `DeleteAsync_ExistingBom_DeletesSuccessfully` [Unit]

### Unit Tests -- ProductAccessoryServiceTests

- `AddAsync_ValidRequest_ReturnsCreatedAccessory` [Unit]
- `AddAsync_SelfReference_ReturnsValidationError` [Unit]
- `AddAsync_Duplicate_ReturnsConflictError` [Unit]
- `RemoveAsync_ExistingLink_RemovesSuccessfully` [Unit]
- `ListAsync_ReturnsAccessories` [Unit]

### Unit Tests -- ProductSubstituteServiceTests

- `AddAsync_ValidRequest_ReturnsCreatedSubstitute` [Unit]
- `AddAsync_SelfReference_ReturnsValidationError` [Unit]
- `AddAsync_Duplicate_ReturnsConflictError` [Unit]
- `RemoveAsync_ExistingLink_RemovesSuccessfully` [Unit]
- `ListAsync_ReturnsSubstitutes` [Unit]

### Unit Tests -- Validation

- `CreateProductRequestValidator_MissingCode_Fails` [Unit]
- `CreateProductRequestValidator_CodeTooLong_Fails` [Unit]
- `CreateProductRequestValidator_MissingName_Fails` [Unit]
- `CreateProductRequestValidator_NameTooLong_Fails` [Unit]
- `CreateProductRequestValidator_MissingUnitOfMeasureId_Fails` [Unit]
- `CreateCategoryRequestValidator_MissingName_Fails` [Unit]
- `CreateCategoryRequestValidator_NameTooLong_Fails` [Unit]
- `CreateUnitOfMeasureRequestValidator_MissingCode_Fails` [Unit]
- `CreateUnitOfMeasureRequestValidator_CodeTooLong_Fails` [Unit]
- `AddBomLineRequestValidator_MissingChildProductId_Fails` [Unit]
- `AddBomLineRequestValidator_ZeroQuantity_Fails` [Unit]

### Integration Tests -- ProductsControllerTests

- `CreateProduct_ValidPayload_Returns201` [Integration]
- `CreateProduct_DuplicateCode_Returns409` [Integration]
- `CreateProduct_InvalidPayload_Returns400ProblemDetails` [Integration]
- `CreateProduct_Unauthenticated_Returns401` [Integration]
- `GetProduct_ExistingId_Returns200` [Integration]
- `GetProduct_NonExistentId_Returns404` [Integration]
- `UpdateProduct_ValidPayload_Returns200` [Integration]
- `DeleteProduct_ActiveProduct_Returns204` [Integration]
- `SearchProducts_WithNameFilter_ReturnsMatchingResults` [Integration]
- `SearchProducts_Pagination_ReturnsCorrectPage` [Integration]
- `ReactivateProduct_SoftDeleted_Returns200` [Integration]

### Integration Tests -- ProductCategoriesControllerTests

- `CreateCategory_ValidPayload_Returns201` [Integration]
- `CreateCategory_DuplicateName_Returns409` [Integration]
- `ListCategories_Returns200` [Integration]
- `UpdateCategory_ValidPayload_Returns200` [Integration]
- `DeleteCategory_WithProducts_Returns409` [Integration]
- `DeleteCategory_Unused_Returns204` [Integration]

### Integration Tests -- UnitsOfMeasureControllerTests

- `CreateUnit_ValidPayload_Returns201` [Integration]
- `CreateUnit_DuplicateCode_Returns409` [Integration]
- `ListUnits_Returns200` [Integration]
- `DeleteUnit_WithProducts_Returns409` [Integration]
- `DeleteUnit_Unused_Returns204` [Integration]

---

## Key Files

- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductsController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductCategoriesController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/UnitsOfMeasureController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/BomController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductAccessoriesController.cs`
- `src/Interfaces/Inventory/Warehouse.Inventory.API/Controllers/ProductSubstitutesController.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/Product.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/ProductCategory.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/UnitOfMeasure.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/BillOfMaterials.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/BomLine.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/ProductAccessory.cs`
- `src/Databases/Warehouse.Inventory.DBModel/Models/ProductSubstitute.cs`
- `src/Databases/Warehouse.Inventory.DBModel/InventoryDbContext.cs`
- `src/Warehouse.ServiceModel/DTOs/Inventory/`
- `src/Warehouse.ServiceModel/Requests/Inventory/`
- `src/Warehouse.Mapping/Profiles/Inventory/`
- `src/Interfaces/Inventory/Warehouse.Inventory.API.Tests/`
