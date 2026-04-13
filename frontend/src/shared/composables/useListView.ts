import { ref, computed, watch, onMounted, type Ref, type ComputedRef } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import type { RouteLocationRaw } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { PaginatedResponse } from '@shared/types/api';
import type { ColumnFilterState } from '@shared/types/filter';

export interface ListViewNavigation<TDto> {
  createRoute?: RouteLocationRaw;
  editRoute?: (item: TDto) => RouteLocationRaw;
  detailRoute?: (item: TDto) => RouteLocationRaw;
}

export interface ListViewConfig<TDto, TSearch> {
  fetchFn: (params: TSearch) => Promise<PaginatedResponse<TDto>>;
  filterFields: string[];
  filterPathMap?: Record<string, string>;
  defaultSort?: { field: string; descending: boolean };
  defaultPageSize?: number;
  defaultParams?: Partial<TSearch>;
  navigation?: ListViewNavigation<TDto>;
}

export interface ListViewReturn<TDto, TSearch> {
  t: ReturnType<typeof useI18n>['t'];
  locale: ReturnType<typeof useI18n>['locale'];
  layout: ReturnType<typeof useLayoutStore>;
  items: Ref<TDto[]>;
  loading: Ref<boolean>;
  totalCount: Ref<number>;
  totalPages: ComputedRef<number>;
  selectedItem: Ref<TDto | null>;
  showFormDialog: Ref<boolean>;
  showDetailDialog: Ref<boolean>;
  searchParams: Ref<TSearch>;
  columnFilters: Record<string, ColumnFilterState | null>;
  filteredItems: ComputedRef<TDto[]>;
  reload: () => Promise<void>;
  handleCreate: () => void;
  handleEdit: (item: TDto) => void;
  handleDetail: (item: TDto) => void;
  handlePageChange: (newPage: number) => void;
  handlePageSizeChange: (newSize: number) => void;
  formatDate: (dateStr: string) => string;
}

/**
 * Base composable for list views with pagination, filtering, sorting,
 * and page/dialog mode navigation.
 *
 * Subcomposables extend by adding entity-specific behavior (delete, reactivate, etc.).
 */
export function useListView<TDto, TSearch extends Record<string, unknown>>(
  config: ListViewConfig<TDto, TSearch>,
): ListViewReturn<TDto, TSearch> {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const items = ref<TDto[]>([]) as Ref<TDto[]>;
  const loading = ref(false);
  const totalCount = ref(0);
  const pageSize = config.defaultPageSize ?? 25;
  const totalPages = computed(() => Math.ceil(totalCount.value / pageSize));
  const selectedItem = ref<TDto | null>(null) as Ref<TDto | null>;
  const showFormDialog = ref(false);
  const showDetailDialog = ref(false);

  const initialParams = {
    ...(config.defaultParams ?? {}),
    sortBy: config.defaultSort?.field,
    sortDescending: config.defaultSort?.descending ?? false,
    page: 1,
    pageSize,
  } as unknown as TSearch;

  const searchParams = ref<TSearch>(initialParams) as Ref<TSearch>;

  const { columnFilters, filteredItems } = useColumnFilters(items, config.filterFields);

  watch(columnFilters, () => {
    const filterStr = buildFilterString(columnFilters, config.filterPathMap);
    searchParams.value = {
      ...searchParams.value,
      filter: filterStr,
      page: 1,
    } as TSearch;
    reload();
  }, { deep: true });

  onMounted(() => reload());

  async function reload(): Promise<void> {
    loading.value = true;
    try {
      const response = await config.fetchFn(searchParams.value);
      items.value = response.items;
      totalCount.value = response.totalCount;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      loading.value = false;
    }
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  function handleCreate(): void {
    if (layout.isPageMode && config.navigation?.createRoute) {
      router.push(config.navigation.createRoute);
    } else {
      selectedItem.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(item: TDto): void {
    if (layout.isPageMode && config.navigation?.editRoute) {
      router.push(config.navigation.editRoute(item));
    } else {
      selectedItem.value = item;
      showFormDialog.value = true;
    }
  }

  function handleDetail(item: TDto): void {
    if (layout.isPageMode && config.navigation?.detailRoute) {
      router.push(config.navigation.detailRoute(item));
    } else {
      selectedItem.value = item;
      showDetailDialog.value = true;
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage } as TSearch;
    reload();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 } as TSearch;
    reload();
  }

  return {
    t,
    locale,
    layout,
    items,
    loading,
    totalCount,
    totalPages,
    selectedItem,
    showFormDialog,
    showDetailDialog,
    searchParams,
    columnFilters,
    filteredItems,
    reload,
    handleCreate,
    handleEdit,
    handleDetail,
    handlePageChange,
    handlePageSizeChange,
    formatDate,
  };
}
