import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchWarehouses, deactivateWarehouse, reactivateWarehouse } from '@features/inventory/api/warehouses';
import type { WarehouseDto, SearchWarehousesRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useWarehousesView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const warehouses = ref<WarehouseDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const selectedWarehouse = ref<WarehouseDto | null>(null);
  const showFormDialog = ref(false);
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const searchParams = ref<SearchWarehousesRequest>({
    includeDeleted: false,
    sortBy: 'name',
    sortDescending: false,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(warehouses, ['name', 'code', 'address']);

  const filterPathMap: Record<string, string> = {
    name: 'name',
    code: 'code',
    address: 'address',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadWarehouses();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('warehouses.columns.code'), key: 'code', sortable: true },
    { title: t('warehouses.columns.name'), key: 'name', sortable: true },
    { title: t('warehouses.columns.address'), key: 'address', sortable: false },
    { title: t('warehouses.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('warehouses.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  onMounted(() => loadWarehouses());

  async function loadWarehouses(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchWarehouses(searchParams.value);
      warehouses.value = response.items;
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
    if (layout.isPageMode) {
      router.push({ name: 'warehouse-create' });
    } else {
      selectedWarehouse.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(warehouse: WarehouseDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'warehouse-edit', params: { id: warehouse.id } });
    } else {
      selectedWarehouse.value = warehouse;
      showFormDialog.value = true;
    }
  }

  function handleDetail(warehouse: WarehouseDto): void {
    router.push({ name: 'warehouse-detail', params: { id: warehouse.id } });
  }

  function openDeactivateDialog(warehouse: WarehouseDto): void {
    selectedWarehouse.value = warehouse;
    showDeactivateDialog.value = true;
  }

  async function handleDeactivate(): Promise<void> {
    if (!selectedWarehouse.value) return;
    deactivating.value = true;
    try {
      await deactivateWarehouse(selectedWarehouse.value.id);
      notification.success(t('warehouses.deactivated'));
      showDeactivateDialog.value = false;
      await loadWarehouses();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deactivating.value = false;
    }
  }

  async function handleReactivate(warehouse: WarehouseDto): Promise<void> {
    try {
      await reactivateWarehouse(warehouse.id);
      notification.success(t('warehouses.reactivated'));
      await loadWarehouses();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadWarehouses();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadWarehouses();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    selectedWarehouse,
    showFormDialog,
    showDeactivateDialog,
    deactivating,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadWarehouses,
    formatDate,
    handleCreate,
    handleEdit,
    handleDetail,
    openDeactivateDialog,
    handleDeactivate,
    handleReactivate,
    handlePageChange,
    handlePageSizeChange,
  };
}
