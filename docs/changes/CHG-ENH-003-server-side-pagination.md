# CHG-ENH-003 — Server-Side Pagination for Unpaginated List Endpoints

> Status: Implemented
> Last updated: 2026-04-09
> Owner: TBD
> Priority: P2

## 1. Context & Scope

**Why this change is needed:**
Several list endpoints return the full dataset as a plain array, which means the frontend loads all records into memory regardless of dataset size. As data grows this will degrade performance and responsiveness. All grid-backed endpoints should support server-side pagination with a default page size of 25.

**Scope:**
- [x] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### 2.1 General Pagination Rules

- All endpoints listed in section 7 MUST accept optional `page` (default 1) and `pageSize` (default 25) query parameters.
- All endpoints listed in section 7 MUST return `PaginatedResponse<T>` instead of `IReadOnlyList<T>`.
- The `PaginatedResponse<T>` contract MUST include: `items`, `page`, `pageSize`, `totalCount`, `totalPages`.
- The system MUST clamp `pageSize` to a maximum of 100.
- The system MUST default `page` to 1 and `pageSize` to 25 when not provided.
- The system MUST return an empty `items` array (not 404) when the page is beyond the total count.

### 2.2 Endpoints to Paginate

The following endpoints MUST be converted from unpaginated to paginated:

| # | API | Endpoint | Current Return |
|---|---|---|---|
| EP1 | Auth | `GET /api/v1/permissions` | `IReadOnlyList<PermissionDto>` |
| EP2 | Auth | `GET /api/v1/roles` | `IReadOnlyList<RoleDto>` |
| EP3 | Customers | `GET /api/v1/customer-categories` | `IReadOnlyList<CustomerCategoryDto>` |
| EP4 | Inventory | `GET /api/v1/product-categories` | `IReadOnlyList<ProductCategoryDto>` |
| EP5 | Inventory | `GET /api/v1/units-of-measure` | `IReadOnlyList<UnitOfMeasureDto>` |

### 2.3 Endpoints to Exclude from Pagination

The following endpoints SHOULD remain unpaginated because they return small, bounded sub-resource collections scoped to a parent entity:

| Endpoint | Reason |
|---|---|
| `GET /api/v1/roles/{id}/permissions` | Permissions for one role — bounded by permission count |
| `GET /api/v1/users/{id}/roles` | Roles for one user — bounded by role count |
| `GET /api/v1/customers/{id}/addresses` | Addresses for one customer — typically < 5 |
| `GET /api/v1/customers/{id}/phones` | Phones for one customer — typically < 5 |
| `GET /api/v1/customers/{id}/emails` | Emails for one customer — typically < 5 |
| `GET /api/v1/customers/{id}/accounts` | Accounts for one customer — typically < 5 |
| `GET /api/v1/bom/by-product/{productId}` | BOM lines for one product — bounded |
| `GET /api/v1/product-accessories/by-product/{productId}` | Accessories for one product — bounded |
| `GET /api/v1/product-substitutes/by-product/{productId}` | Substitutes for one product — bounded |
| `GET /api/v1/stocktake/{id}/counts` | Counts for one session — scoped |
| `GET /api/v1/stocktake/{id}/variance` | Variance report for one session — scoped |

### 2.4 Frontend Changes

- All frontend grids that call the endpoints in section 2.2 MUST use server-side pagination with a default page size of 25.
- The frontend MUST send `page` and `pageSize` query parameters on every list request.
- The frontend MUST display pagination controls (page navigation, total count).
- The frontend API client types MUST be updated from `T[]` to `PaginatedResponse<T>` for affected endpoints.
- Grids that already use `PaginatedResponse<T>` MUST have their default page size standardized to 25.

## 3. Validation Rules

| # | Field / Input | Rule | Error Code |
|---|---|---|---|
| V1 | `page` | Must be >= 1 when provided. Default: 1. | `INVALID_PAGE` |
| V2 | `pageSize` | Must be >= 1 and <= 100 when provided. Default: 25. | `INVALID_PAGE_SIZE` |

## 4. Error Rules

| # | Condition | HTTP Status | Error Code | Message |
|---|---|---|---|---|
| E1 | `page` < 1 | 400 | `INVALID_PAGE` | Page must be greater than or equal to 1. |
| E2 | `pageSize` < 1 or > 100 | 400 | `INVALID_PAGE_SIZE` | Page size must be between 1 and 100. |

## 5. Versioning Notes

**API version impact:** None — additive query parameters with defaults, same v1 routes.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — existing callers that omit `page`/`pageSize` will receive the first 25 items (behavior change from receiving all items). Frontend will be updated simultaneously.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] PermissionService_GetAll_ReturnsPaginated` — returns paginated response with correct totalCount
- [ ] `[Unit] RoleService_GetAll_ReturnsPaginated` — returns paginated response with correct totalCount
- [ ] `[Unit] CustomerCategoryService_GetAll_ReturnsPaginated` — returns paginated response
- [ ] `[Unit] ProductCategoryService_List_ReturnsPaginated` — returns paginated response
- [ ] `[Unit] UnitOfMeasureService_List_ReturnsPaginated` — returns paginated response
- [ ] `[Unit] PaginationParams_DefaultValues_Page1Size25` — verifies defaults
- [ ] `[Unit] PaginationParams_PageSizeExceeds100_ClampedTo100` — verifies max clamping

### Integration Tests

- [ ] `[Integration] GetPermissions_NoPaginationParams_ReturnsFirst25` — default pagination
- [ ] `[Integration] GetPermissions_Page2Size10_ReturnsCorrectSlice` — explicit pagination
- [ ] `[Integration] GetRoles_NoPaginationParams_ReturnsFirst25` — default pagination
- [ ] `[Integration] GetProductCategories_WithPagination_ReturnsPaginatedResponse` — response shape
- [ ] `[Integration] GetUnitsOfMeasure_PageBeyondTotal_ReturnsEmptyItems` — empty page

## 7. Detailed Design

### API Changes

Each endpoint in section 2.2 changes from:

```
GET /api/v1/{resource}
Response: [ { ... }, { ... } ]
```

To:

```
GET /api/v1/{resource}?page=1&pageSize=25
Response: {
  "items": [ { ... }, { ... } ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 42,
  "totalPages": 2
}
```

### Data Model Changes

None — no schema changes required.

### Service Layer Changes

For each affected service:

1. Add `page` and `pageSize` parameters (or a shared `PaginationParams` record) to the list/get-all method.
2. Apply `.Skip((page - 1) * pageSize).Take(pageSize)` to the query.
3. Execute a `.CountAsync()` for `totalCount`.
4. Return `PaginatedResponse<T>` using the existing response model.

Consider extracting a shared `PaginationParams` record in `Warehouse.Common` if one does not already exist:

```csharp
public sealed record PaginationParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
```

### Frontend Changes

For each affected grid:

1. Update the API client function to accept `page`/`pageSize` params and expect `PaginatedResponse<T>`.
2. Update the composable to track `page`, `pageSize`, `totalCount`.
3. Replace local `v-data-table` with server-side pagination: bind `:items-length`, `:page`, `:items-per-page`, and handle `@update:page` / `@update:items-per-page` events.
4. Remove client-side filtering on these grids if it conflicts with server-side pagination.

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-AUTH-001 | Permissions list and Roles list endpoints change return type to PaginatedResponse |
| SDD-CUST-001 | Customer categories endpoint changes return type to PaginatedResponse |
| SDD-INV-001 | Product categories and units of measure endpoints change return type to PaginatedResponse |

## Migration Plan

1. **Pre-deployment:** No prerequisites.
2. **Deployment:** Deploy backend and frontend together — frontend expects PaginatedResponse on affected endpoints.
3. **Post-deployment:** Verify all grids load with 25-record default and pagination controls work.
4. **Rollback:** Revert both backend and frontend simultaneously.

## Open Questions

- [ ] Should the existing `SearchCustomersRequest`, `SearchProductsRequest`, etc. have their default `pageSize` changed from 20 to 25 for consistency, or keep existing defaults?
- [ ] Should unpaginated sub-resource endpoints (addresses, phones, etc.) be paginated in a future enhancement if data volume grows?
