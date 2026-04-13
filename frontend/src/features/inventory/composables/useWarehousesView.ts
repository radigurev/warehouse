import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useListView } from '@shared/composables/useListView';
import { searchWarehouses, deactivateWarehouse, reactivateWarehouse } from '@features/inventory/api/warehouses';
import type { WarehouseDto, SearchWarehousesRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';
import { computed } from 'vue';

export function useWarehousesView() {
  const { t } = useI18n();
  const router = useRouter();
  const notification = useNotificationStore();

  const base = useListView<WarehouseDto, SearchWarehousesRequest>({
    fetchFn: searchWarehouses,
    filterFields: ['name', 'code', 'address'],
    filterPathMap: { name: 'name', code: 'code', address: 'address' },
    defaultSort: { field: 'name', descending: false },
    defaultParams: { includeDeleted: false } as Partial<SearchWarehousesRequest>,
    navigation: {
      createRoute: { name: 'warehouse-create' },
      editRoute: (w: WarehouseDto) => ({ name: 'warehouse-edit', params: { id: w.id } }),
    },
  });

  const selectedWarehouse = base.selectedItem;
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const headers = computed(() => [
    { title: t('warehouses.columns.code'), key: 'code', sortable: true },
    { title: t('warehouses.columns.name'), key: 'name', sortable: true },
    { title: t('warehouses.columns.address'), key: 'address', sortable: false },
    { title: t('warehouses.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('warehouses.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

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
      await base.reload();
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
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    ...base,
    selectedWarehouse,
    showDeactivateDialog,
    deactivating,
    headers,
    loadWarehouses: base.reload,
    handleDetail,
    openDeactivateDialog,
    handleDeactivate,
    handleReactivate,
  };
}
