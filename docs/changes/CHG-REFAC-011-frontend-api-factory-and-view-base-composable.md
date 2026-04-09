# CHG-REFAC-011 — Frontend: API Factory & View Base Composable

> Status: Draft
> Last updated: 2026-04-09
> Owner: TBD
> Priority: P1

## 1. Context & Scope

**Why this change is needed:**
Two major sources of duplication in the Vue.js frontend: (1) 30+ CRUD API functions across all `api/*.ts` files repeat the same `apiClient.get/post/put/delete(...).then(r => r.data)` pattern, differing only by endpoint path and types. Adding a new resource requires copying and adapting ~25 lines of boilerplate. (2) 11+ list view composables (`useProductsView`, `useWarehousesView`, `useCustomersView`, etc.) re-implement identical pagination, loading, error handling, column filters, and page/dialog navigation logic. Each composable is ~150-200 lines, of which ~100 are boilerplate. For an extensible frontend, new features should get standard CRUD and list view behavior out of the box.

**Scope:**
- [ ] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### CRUD API Factory

- The system MUST provide a `createCrudApi<TDto, TDetail, TCreate, TUpdate, TSearch>` factory function that generates standard CRUD operations for a given endpoint.
- The factory MUST generate: `search(params): Promise<PaginatedResponse<TDto>>`, `getById(id): Promise<TDetail>`, `create(request): Promise<TDetail>`, `update(id, request): Promise<TDetail>`, `delete(id): Promise<void>`.
- Each generated function MUST use the shared `apiClient` instance.
- The factory MUST accept the base endpoint path as a parameter (e.g., `/api/v1/products`).
- The factory SHOULD accept an optional configuration object for non-standard endpoints (e.g., nested resources like `/customers/{id}/emails`).
- Existing per-resource API files MUST use the factory for standard CRUD and add only non-standard endpoints manually.
- Adding a new resource's standard CRUD API MUST require only: `export const productApi = createCrudApi<ProductDto, ...>('/api/v1/products')`.

### List View Base Composable

- The system MUST provide a `useListView<TDto, TSearch>` base composable that encapsulates:
  - Reactive state: `items`, `loading`, `page`, `pageSize`, `totalCount`, `totalPages`
  - Pagination handlers: `handlePageChange`, `handlePageSizeChange`
  - Error handling: try-catch with `notification.error(getApiErrorMessage(err, t))`
  - Column filters: `useColumnFilters(items, fields)` integration
  - Breadcrumb setup via `layout.setBreadcrumbs`
  - Search parameter construction with filter integration
- The base composable MUST accept configuration:
  - `fetchFn: (params: TSearch) => Promise<PaginatedResponse<TDto>>` — the API search function
  - `filterFields: string[]` — columns available for client-side filtering
  - `defaultSort: { field: string, descending: boolean }` — initial sort configuration
  - `defaultPageSize?: number` — initial page size (default 25)
- Subcomposables MUST extend by adding entity-specific behavior: navigation routes, dialog state, delete confirmation, custom computed properties.
- The base MUST expose a `reload()` function that re-fetches with current parameters.
- The base MUST call `fetchFn` on mount and on pagination/sort/filter changes.
- Adding a new list view MUST require only: calling `useListView` with the API function, filter fields, and adding entity-specific handlers (~30 lines instead of ~150).

### Page/Dialog Navigation

- The base composable MUST accept an optional `navigation` configuration:
  - `createRoute?: RouteLocationRaw` — route for create in page mode
  - `editRoute?: (item: TDto) => RouteLocationRaw` — route builder for edit in page mode
  - `detailRoute?: (item: TDto) => RouteLocationRaw` — route builder for detail in page mode
- When `layout.isPageMode` is true, navigation MUST use `router.push` with the configured routes.
- When `layout.isPageMode` is false (dialog mode), navigation MUST set `selectedItem` and `showFormDialog`/`showDetailDialog` refs.
- The base MUST expose: `handleCreate()`, `handleEdit(item)`, `handleDetail(item)` that internally check layout mode.

## 3. Validation Rules

No validation rules — this is a frontend structural refactor.

## 4. Error Rules

No new error rules — error handling behavior preserved.

## 5. Versioning Notes

**API version impact:** None — frontend-only refactor.

**Database migration required:** No

**Backwards compatibility:** Fully compatible — no API changes.

## 6. Test Plan

### Unit Tests

- [ ] `[Unit] createCrudApi_search_CallsGetWithParams` — verify search delegation
- [ ] `[Unit] createCrudApi_getById_CallsGetWithId` — verify getById delegation
- [ ] `[Unit] createCrudApi_create_CallsPostWithBody` — verify create delegation
- [ ] `[Unit] createCrudApi_update_CallsPutWithIdAndBody` — verify update delegation
- [ ] `[Unit] createCrudApi_delete_CallsDeleteWithId` — verify delete delegation
- [ ] `[Unit] useListView_LoadsDataOnMount` — verify initial fetch
- [ ] `[Unit] useListView_HandlePageChange_UpdatesPageAndReloads` — verify pagination
- [ ] `[Unit] useListView_HandlePageSizeChange_ResetsPageAndReloads` — verify page size reset
- [ ] `[Unit] useListView_ErrorHandling_ShowsNotification` — verify error notification
- [ ] `[Unit] useListView_PageMode_NavigatesToRoute` — verify page mode navigation
- [ ] `[Unit] useListView_DialogMode_SetsDialogState` — verify dialog mode navigation

### Integration Tests

- [ ] `[Integration] ProductsView_UsingBaseComposable_LoadsAndPaginates` — end-to-end
- [ ] `[Integration] CustomersView_UsingBaseComposable_LoadsAndPaginates` — end-to-end

## 7. Detailed Design

### CRUD API Factory

```typescript
// frontend/src/shared/api/createCrudApi.ts
export interface CrudApi<TDto, TDetail, TCreate, TUpdate, TSearch> {
  search(params: TSearch): Promise<PaginatedResponse<TDto>>;
  getById(id: number): Promise<TDetail>;
  create(request: TCreate): Promise<TDetail>;
  update(id: number, request: TUpdate): Promise<TDetail>;
  remove(id: number): Promise<void>;
}

export function createCrudApi<TDto, TDetail, TCreate, TUpdate, TSearch>(
  basePath: string,
): CrudApi<TDto, TDetail, TCreate, TUpdate, TSearch> {
  return {
    search: (params) => apiClient.get(basePath, { params }).then((r) => r.data),
    getById: (id) => apiClient.get(`${basePath}/${id}`).then((r) => r.data),
    create: (request) => apiClient.post(basePath, request).then((r) => r.data),
    update: (id, request) => apiClient.put(`${basePath}/${id}`, request).then((r) => r.data),
    remove: (id) => apiClient.delete(`${basePath}/${id}`).then(() => undefined),
  };
}
```

### Usage Example

```typescript
// frontend/src/features/inventory/api/products.ts
import { createCrudApi } from '@/shared/api/createCrudApi';

const base = createCrudApi<ProductDto, ProductDetailDto, CreateProductRequest, UpdateProductRequest, SearchProductsRequest>(
  '/api/v1/products',
);

export const { search: searchProducts, getById: getProductById, create: createProduct, update: updateProduct, remove: deleteProduct } = base;

// Non-standard endpoints remain manual:
export function getProductAccessories(productId: number): Promise<ProductAccessoryDto[]> {
  return apiClient.get(`/api/v1/products/${productId}/accessories`).then((r) => r.data);
}
```

### List View Base Composable

```typescript
// frontend/src/shared/composables/useListView.ts
export interface ListViewConfig<TDto, TSearch> {
  fetchFn: (params: TSearch) => Promise<PaginatedResponse<TDto>>;
  filterFields: string[];
  defaultSort: { field: string; descending: boolean };
  defaultPageSize?: number;
  navigation?: {
    createRoute?: RouteLocationRaw;
    editRoute?: (item: TDto) => RouteLocationRaw;
    detailRoute?: (item: TDto) => RouteLocationRaw;
  };
}

export function useListView<TDto, TSearch extends PaginationParams>(
  config: ListViewConfig<TDto, TSearch>,
) {
  // Returns: items, loading, page, pageSize, totalCount, totalPages,
  // columnFilters, filteredItems, selectedItem, showFormDialog, showDetailDialog,
  // handlePageChange, handlePageSizeChange, handleCreate, handleEdit, handleDetail, reload
}
```

### Composable Usage Example

```typescript
// frontend/src/features/inventory/composables/useProductsView.ts
export function useProductsView() {
  const base = useListView<ProductDto, SearchProductsRequest>({
    fetchFn: searchProducts,
    filterFields: ['name', 'code', 'sku'],
    defaultSort: { field: 'name', descending: false },
    navigation: {
      createRoute: { name: 'product-create' },
      editRoute: (p) => ({ name: 'product-edit', params: { id: p.id } }),
      detailRoute: (p) => ({ name: 'product-detail', params: { id: p.id } }),
    },
  });

  // Entity-specific additions:
  async function handleDelete(product: ProductDto): Promise<void> {
    await deleteProduct(product.id);
    base.reload();
  }

  return { ...base, handleDelete };
}
```

### Files Affected

**API files refactored to use factory (30+ functions reduced):**
- `features/auth/api/users.ts`, `roles.ts`, `permissions.ts`
- `features/customers/api/customers.ts`, `categories.ts`
- `features/inventory/api/products.ts`, `warehouses.ts`, `zones.ts`, `storage-locations.ts`, `stock-levels.ts`, `stock-movements.ts`, `batches.ts`, `units-of-measure.ts`, `product-categories.ts`, `adjustments.ts`, `transfers.ts`, `stocktakes.ts`
- `features/purchasing/api/*.ts` (when implemented)
- `features/fulfillment/api/*.ts` (when implemented)

**Composables refactored to use base (11+ composables simplified):**
- `useUsersView`, `useRolesView`, `usePermissionsView`, `useAuditView`
- `useCustomersView`, `useCategoriesView`
- `useProductsView`, `useWarehousesView`, `useBatchesView`, `useProductCategoriesView`, `useUnitsOfMeasureView`, `useStockLevelsView`

### Files Created

- `frontend/src/shared/api/createCrudApi.ts`
- `frontend/src/shared/composables/useListView.ts`

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-UI-001 | No behavior change — frontend refactored to use factory and base composable |
| SDD-UI-002 | No behavior change — list views refactored to use base composable |

## Migration Plan

1. **Pre-deployment:** Implement `createCrudApi` and `useListView`. Migrate one feature (e.g., Products) as proof of concept. Verify all list/CRUD operations work. Migrate remaining features.
2. **Deployment:** Deploy updated frontend — no backend changes.
3. **Post-deployment:** Verify all list views load, paginate, sort, filter, and navigate correctly.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should `createCrudApi` support nested resource paths (e.g., `/customers/{id}/emails`) via a second parameter, or should those remain manual?
- [ ] Should `useListView` support multiple selection for bulk operations?
- [ ] Should the base composable handle `onMounted` internally, or should the calling composable trigger the initial load?
