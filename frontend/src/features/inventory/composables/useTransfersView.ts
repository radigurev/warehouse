import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchTransfers } from '@features/inventory/api/transfers';
import type { WarehouseTransferDto, SearchTransfersRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useTransfersView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const transfers = ref<WarehouseTransferDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));

  const searchParams = ref<SearchTransfersRequest>({
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(transfers, ['sourceWarehouseName', 'destinationWarehouseName', 'status']);

  const filterPathMap: Record<string, string> = {
    sourceWarehouseName: 'sourceWarehouse.name',
    destinationWarehouseName: 'destinationWarehouse.name',
    status: 'status',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadTransfers();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('transfers.columns.id'), key: 'id', sortable: true },
    { title: t('transfers.columns.sourceWarehouse'), key: 'sourceWarehouseName', sortable: false },
    { title: t('transfers.columns.destinationWarehouse'), key: 'destinationWarehouseName', sortable: false },
    { title: t('transfers.columns.status'), key: 'status', sortable: true },
    { title: t('transfers.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadTransfers());

  async function loadTransfers(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchTransfers(searchParams.value);
      transfers.value = response.items;
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
    router.push({ name: 'transfer-create' });
  }

  function handleDetail(transfer: WarehouseTransferDto): void {
    router.push({ name: 'transfer-detail', params: { id: transfer.id } });
  }

  function statusColor(status: string): string {
    switch (status) {
      case 'Draft': return 'warning';
      case 'Completed': return 'success';
      case 'Cancelled': return 'error';
      default: return 'grey';
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadTransfers();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadTransfers();
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
    loadTransfers,
    formatDate,
    handleCreate,
    handleDetail,
    statusColor,
    handlePageChange,
    handlePageSizeChange,
  };
}
