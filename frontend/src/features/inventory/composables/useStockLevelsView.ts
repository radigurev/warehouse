import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchStockLevels } from '@features/inventory/api/stock-levels';
import type { StockLevelDto, SearchStockLevelsRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useStockLevelsView() {
  const { t, locale } = useI18n();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const stockLevels = ref<StockLevelDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));

  const searchParams = ref<SearchStockLevelsRequest>({
    sortBy: 'productName',
    sortDescending: false,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(stockLevels, [
    'productName',
    'productCode',
    'warehouseName',
    'locationName',
  ]);

  const filterPathMap: Record<string, string> = {
    productName: 'product.name',
    productCode: 'product.code',
    warehouseName: 'warehouse.name',
    locationName: 'location.name',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadStockLevels();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('stockLevels.columns.productCode'), key: 'productCode', sortable: true },
    { title: t('stockLevels.columns.product'), key: 'productName', sortable: true },
    { title: t('stockLevels.columns.warehouse'), key: 'warehouseName', sortable: true },
    { title: t('stockLevels.columns.location'), key: 'locationName', sortable: false },
    { title: t('stockLevels.columns.onHand'), key: 'quantityOnHand', sortable: true, align: 'end' as const },
    { title: t('stockLevels.columns.reserved'), key: 'reservedQuantity', sortable: true, align: 'end' as const },
    { title: t('stockLevels.columns.available'), key: 'availableQuantity', sortable: true, align: 'end' as const },
  ]);

  onMounted(() => loadStockLevels());

  async function loadStockLevels(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchStockLevels(searchParams.value);
      stockLevels.value = response.items;
      totalCount.value = response.totalCount;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      loading.value = false;
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadStockLevels();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadStockLevels();
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
    loadStockLevels,
    handlePageChange,
    handlePageSizeChange,
  };
}
