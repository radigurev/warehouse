# CHG-REFAC-012 — Frontend: Navigation Strategy & Utility Patterns

> Status: Implemented
> Last updated: 2026-04-13
> Owner: TBD
> Priority: P3

## 1. Context & Scope

**Why this change is needed:**
Three frontend patterns create maintenance burden as the application grows: (1) Page mode vs dialog mode navigation — 15+ composables duplicate `if (layout.isPageMode) { router.push(...) } else { showFormDialog.value = true }` for create, edit, and detail actions. Adding a new layout mode (e.g., side panel, split view) would require modifying every composable. (2) Search parameter construction — 5+ composables manually build and modify `searchParams` objects with watchers for filter integration, each repeating the same spread-and-reset pattern. (3) Token refresh interceptor — the Axios response interceptor in `client.ts` is a 70-line monolith mixing token refresh, queue management, permission refresh, and redirect logic. These patterns should be extracted into reusable, strategy-based utilities.

**Scope:**
- [ ] Backend API changes
- [ ] Database schema changes
- [x] Frontend changes
- [ ] Configuration changes

## 2. Behavior (RFC 2119)

### Navigation Strategy

- The system MUST define a `NavigationStrategy` interface with methods: `navigateToCreate()`, `navigateToEdit(item)`, `navigateToDetail(item)`.
- The system MUST provide two implementations: `PageModeStrategy` (uses `router.push`) and `DialogModeStrategy` (sets dialog refs).
- The system MUST provide a `useNavigationStrategy` composable that returns the appropriate strategy based on `layout.isPageMode`.
- The strategy MUST reactively switch when the user toggles layout mode.
- View composables MUST call `strategy.navigateToCreate()` instead of inline if-else checks.
- Adding a new layout mode (e.g., `SidePanelStrategy`) MUST require only: creating a new strategy implementation and updating the strategy resolver.

### Search Parameter Builder

- The system MUST provide a `useSearchParams<TSearch>` composable that manages reactive search parameters.
- The composable MUST accept: default values, sort configuration, and filter field mappings.
- The composable MUST expose: `params` (reactive), `updateSort(field, descending)`, `updatePage(page)`, `updatePageSize(size)`, `applyFilters(columnFilters)`.
- When `applyFilters` is called, the composable MUST rebuild the filter string and reset page to 1.
- When `updatePageSize` is called, the composable MUST reset page to 1.
- The composable MUST emit a `change` event (or call a callback) whenever params change, so the view can re-fetch.
- View composables MUST use `useSearchParams` instead of manually managing `searchParams` refs and watchers.

### Token Refresh Facade

- The system MUST extract the Axios response interceptor into a `TokenRefreshManager` class or composable.
- The manager MUST encapsulate: refresh state (`isRefreshing`), failed request queue (`failedQueue`), queue processing, and retry logic.
- The manager MUST separate concerns: `attemptRefresh()`, `enqueueFailedRequest(config)`, `processQueue(token)`, `handleAuthFailure()`.
- The manager MUST handle permission refresh after successful token refresh.
- The Axios interceptor MUST delegate to the manager — the interceptor itself SHOULD be under 10 lines.
- Adding new post-refresh logic (e.g., refreshing user profile) MUST require only: adding a hook to the manager — not modifying the interceptor.

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

- [ ] `[Unit] PageModeStrategy_NavigateToCreate_CallsRouterPush` — verify page navigation
- [ ] `[Unit] DialogModeStrategy_NavigateToCreate_SetsDialogState` — verify dialog navigation
- [ ] `[Unit] useNavigationStrategy_ReturnsPageStrategy_WhenPageMode` — verify strategy resolution
- [ ] `[Unit] useNavigationStrategy_ReturnsDialogStrategy_WhenDialogMode` — verify strategy resolution
- [ ] `[Unit] useNavigationStrategy_SwitchesReactively` — verify mode toggle
- [ ] `[Unit] useSearchParams_UpdatePage_SetsPageValue` — verify page update
- [ ] `[Unit] useSearchParams_UpdatePageSize_ResetsPageToOne` — verify page reset
- [ ] `[Unit] useSearchParams_ApplyFilters_RebuildsFilterAndResetsPage` — verify filter application
- [ ] `[Unit] useSearchParams_EmitsChange_OnAnyUpdate` — verify change callback
- [ ] `[Unit] TokenRefreshManager_EnqueuesRequests_DuringRefresh` — verify queue
- [ ] `[Unit] TokenRefreshManager_ProcessesQueue_AfterRefresh` — verify retry
- [ ] `[Unit] TokenRefreshManager_RedirectsToLogin_OnRefreshFailure` — verify auth failure

### Integration Tests

- [ ] `[Integration] NavigationStrategy_PageMode_RoutesCorrectly` — end-to-end navigation
- [ ] `[Integration] TokenRefreshManager_RefreshesAndRetries_SuccessScenario` — end-to-end

## 7. Detailed Design

### Navigation Strategy

```typescript
// frontend/src/shared/composables/useNavigationStrategy.ts
export interface NavigationStrategy<TItem> {
  navigateToCreate(): void;
  navigateToEdit(item: TItem): void;
  navigateToDetail(item: TItem): void;
}

export interface NavigationConfig<TItem> {
  routes: {
    create?: RouteLocationRaw;
    edit?: (item: TItem) => RouteLocationRaw;
    detail?: (item: TItem) => RouteLocationRaw;
  };
  dialogs: {
    selectedItem: Ref<TItem | null>;
    showFormDialog: Ref<boolean>;
    showDetailDialog: Ref<boolean>;
  };
}

export function useNavigationStrategy<TItem>(
  config: NavigationConfig<TItem>,
): ComputedRef<NavigationStrategy<TItem>> {
  const router = useRouter();
  const layout = useLayoutStore();

  return computed(() => {
    if (layout.isPageMode) {
      return {
        navigateToCreate: () => router.push(config.routes.create!),
        navigateToEdit: (item) => router.push(config.routes.edit!(item)),
        navigateToDetail: (item) => router.push(config.routes.detail!(item)),
      };
    }
    return {
      navigateToCreate: () => {
        config.dialogs.selectedItem.value = null;
        config.dialogs.showFormDialog.value = true;
      },
      navigateToEdit: (item) => {
        config.dialogs.selectedItem.value = item;
        config.dialogs.showFormDialog.value = true;
      },
      navigateToDetail: (item) => {
        config.dialogs.selectedItem.value = item;
        config.dialogs.showDetailDialog.value = true;
      },
    };
  });
}
```

### Search Parameter Builder

```typescript
// frontend/src/shared/composables/useSearchParams.ts
export interface SearchParamsConfig<TSearch> {
  defaults: TSearch;
  filterPathMap?: Record<string, string>;
  onChange: () => void;
}

export function useSearchParams<TSearch extends PaginationParams>(
  config: SearchParamsConfig<TSearch>,
) {
  const params = ref<TSearch>({ ...config.defaults });

  function updatePage(page: number): void {
    params.value = { ...params.value, page };
    config.onChange();
  }

  function updatePageSize(size: number): void {
    params.value = { ...params.value, pageSize: size, page: 1 };
    config.onChange();
  }

  function applyFilters(columnFilters: Record<string, string>): void {
    const filter = buildFilterString(columnFilters, config.filterPathMap ?? {});
    params.value = { ...params.value, filter, page: 1 };
    config.onChange();
  }

  function updateSort(field: string, descending: boolean): void {
    params.value = { ...params.value, sortBy: field, sortDescending: descending, page: 1 };
    config.onChange();
  }

  return { params, updatePage, updatePageSize, applyFilters, updateSort };
}
```

### Token Refresh Facade

```typescript
// frontend/src/shared/api/tokenRefreshManager.ts
export class TokenRefreshManager {
  private isRefreshing = false;
  private failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (error: unknown) => void;
  }> = [];

  async handleUnauthorized(failedRequest: AxiosRequestConfig): Promise<AxiosResponse> {
    if (this.isRefreshing) {
      return this.enqueueRequest(failedRequest);
    }

    this.isRefreshing = true;
    try {
      const newToken = await this.attemptRefresh();
      this.processQueue(null, newToken);
      return this.retryRequest(failedRequest, newToken);
    } catch (error) {
      this.processQueue(error, null);
      this.handleAuthFailure();
      throw error;
    } finally {
      this.isRefreshing = false;
    }
  }

  private async attemptRefresh(): Promise<string> { /* refresh token API call */ }
  private processQueue(error: unknown, token: string | null): void { /* resolve/reject queued */ }
  private handleAuthFailure(): void { /* clear auth state, redirect to login */ }
  private enqueueRequest(config: AxiosRequestConfig): Promise<AxiosResponse> { /* add to queue */ }
  private retryRequest(config: AxiosRequestConfig, token: string): Promise<AxiosResponse> { /* retry */ }
}
```

### Files Affected

**Composables refactored to use navigation strategy (15+ navigation blocks):**
- `useUsersView`, `useRolesView`
- `useCustomersView`, `useCategoriesView`
- `useProductsView`, `useWarehousesView`, `useBatchesView`, `useProductCategoriesView`, `useUnitsOfMeasureView`, `useStockLevelsView`
- Future: `usePurchaseOrdersView`, `useSalesOrdersView`, etc.

**Composables refactored to use search params builder (5+ composables):**
- `useCustomersView`, `useProductsView`, `useWarehousesView`, `useBatchesView`, `useStockLevelsView`

**Client refactored:**
- `frontend/src/shared/api/client.ts` — interceptor delegates to `TokenRefreshManager`

### Files Created

- `frontend/src/shared/composables/useNavigationStrategy.ts`
- `frontend/src/shared/composables/useSearchParams.ts`
- `frontend/src/shared/api/tokenRefreshManager.ts`

## Affected System Specs

| Spec ID | Impact |
|---|---|
| SDD-UI-001 | No behavior change — navigation refactored to use strategy |
| SDD-UI-003 | No behavior change — auth token refresh refactored to facade |

## Migration Plan

1. **Pre-deployment:** Implement the three utilities. Migrate one composable (e.g., `useProductsView`) as proof of concept. Verify navigation, search, and token refresh work. Migrate remaining composables.
2. **Deployment:** Deploy updated frontend — no backend changes.
3. **Post-deployment:** Verify all views navigate correctly in both page and dialog modes. Verify token refresh works when access token expires.
4. **Rollback:** Revert to previous commit — no data migration needed.

## Open Questions

- [ ] Should `useNavigationStrategy` be merged into `useListView` (CHG-REFAC-011) or kept separate for composability?
- [ ] Should the `TokenRefreshManager` support pluggable post-refresh hooks (e.g., refresh user profile, refresh permissions)?
- [ ] Should `useSearchParams` support debounced filter application for text input filters?
