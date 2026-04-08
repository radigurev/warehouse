import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchSessions } from '@features/inventory/api/stocktake';
import type { StocktakeSessionDto, SearchStocktakeSessionsRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useStocktakeView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const sessions = ref<StocktakeSessionDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));

  const searchParams = ref<SearchStocktakeSessionsRequest>({
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(sessions, [
    'name',
    'warehouseName',
    'status',
  ]);

  const filterPathMap: Record<string, string> = {
    name: 'name',
    warehouseName: 'warehouse.name',
    status: 'status',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadSessions();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('stocktake.columns.name'), key: 'name', sortable: true },
    { title: t('stocktake.columns.status'), key: 'status', sortable: true },
    { title: t('stocktake.columns.warehouse'), key: 'warehouseName', sortable: true },
    { title: t('stocktake.columns.zone'), key: 'zoneName', sortable: false },
    { title: t('stocktake.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadSessions());

  async function loadSessions(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchSessions(searchParams.value);
      sessions.value = response.items;
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

  function translateStatus(status: string): string {
    const key = `stocktake.statuses.${status}`;
    const translated = t(key);
    return translated !== key ? translated : status;
  }

  function statusColor(status: string): string {
    switch (status) {
      case 'Draft': return 'default';
      case 'InProgress': return 'info';
      case 'Completed': return 'success';
      case 'Cancelled': return 'error';
      default: return 'default';
    }
  }

  function handleCreate(): void {
    router.push({ name: 'stocktake-create' });
  }

  function handleDetail(session: StocktakeSessionDto): void {
    router.push({ name: 'stocktake-detail', params: { id: session.id } });
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadSessions();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadSessions();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadSessions,
    formatDate,
    translateStatus,
    statusColor,
    handleCreate,
    handleDetail,
    handlePageChange,
    handlePageSizeChange,
  };
}
