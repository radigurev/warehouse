import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchMovements, recordMovement } from '@features/inventory/api/stock-movements';
import type {
  StockMovementDto,
  SearchStockMovementsRequest,
  RecordStockMovementRequest,
} from '@features/inventory/types/inventory';

export function useStockMovementsView() {
  const { t, locale } = useI18n();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const movements = ref<StockMovementDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const showFormDialog = ref(false);

  const searchParams = ref<SearchStockMovementsRequest>({
    sortBy: 'createdAtUtc',
    sortDescending: true,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(movements, [
    'productName',
    'warehouseName',
    'reasonCode',
  ]);

  const filterPathMap: Record<string, string> = {
    productName: 'product.name',
    warehouseName: 'warehouse.name',
    reasonCode: 'reasonCode',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadMovements();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('stockMovements.columns.product'), key: 'productName', sortable: true },
    { title: t('stockMovements.columns.warehouse'), key: 'warehouseName', sortable: true },
    { title: t('stockMovements.columns.location'), key: 'locationName', sortable: false },
    { title: t('stockMovements.columns.quantity'), key: 'quantity', sortable: true, align: 'end' as const },
    { title: t('stockMovements.columns.reason'), key: 'reasonCode', sortable: true },
    { title: t('stockMovements.columns.reference'), key: 'referenceNumber', sortable: false },
    { title: t('stockMovements.columns.createdAt'), key: 'createdAtUtc', sortable: true },
  ]);

  onMounted(() => loadMovements());

  async function loadMovements(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchMovements(searchParams.value);
      movements.value = response.items;
      totalCount.value = response.totalCount;
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      loading.value = false;
    }
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  function translateReason(code: string): string {
    const key = `stockMovements.reasons.${code}`;
    const translated = t(key);
    return translated !== key ? translated : code;
  }

  function handleRecordMovement(): void {
    showFormDialog.value = true;
  }

  async function submitMovement(request: RecordStockMovementRequest): Promise<void> {
    await recordMovement(request);
    notification.success(t('stockMovements.record') + ' \u2713');
    showFormDialog.value = false;
    await loadMovements();
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadMovements();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadMovements();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    showFormDialog,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadMovements,
    formatDate,
    translateReason,
    handleRecordMovement,
    submitMovement,
    handlePageChange,
    handlePageSizeChange,
  };
}
