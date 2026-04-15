import { ref } from 'vue';
import { useListView } from '@shared/composables/useListView';
import { searchBatches, deactivateBatch } from '@features/inventory/api/batches';
import type { BatchDto, SearchBatchesRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';
import { computed } from 'vue';

export function useBatchesView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const base = useListView<BatchDto, SearchBatchesRequest>({
    fetchFn: searchBatches,
    filterFields: ['batchNumber', 'productName'],
    filterPathMap: { batchNumber: 'batchNumber', productName: 'product.name' },
    defaultParams: { includeExpired: true } as Partial<SearchBatchesRequest>,
    navigation: {
      createRoute: { name: 'batch-create' },
      editRoute: (b: BatchDto) => ({ name: 'batch-edit', params: { id: b.id } }),
    },
  });

  const selectedBatch = base.selectedItem;
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const headers = computed(() => [
    { title: t('batches.columns.batchNumber'), key: 'batchNumber', sortable: true },
    { title: t('batches.columns.product'), key: 'productName', sortable: true },
    { title: t('batches.columns.quantityOnHand'), key: 'quantityOnHand', sortable: true },
    { title: t('batches.columns.expiryDate'), key: 'expiryDate', sortable: true },
    { title: t('batches.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('batches.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  function openDeactivateDialog(batch: BatchDto): void {
    selectedBatch.value = batch;
    showDeactivateDialog.value = true;
  }

  async function handleDeactivate(): Promise<void> {
    if (!selectedBatch.value) return;
    deactivating.value = true;
    try {
      await deactivateBatch(selectedBatch.value.id);
      notification.success(t('batches.deactivate') + ' \u2713');
      showDeactivateDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deactivating.value = false;
    }
  }

  return {
    ...base,
    selectedBatch,
    showDeactivateDialog,
    deactivating,
    headers,
    loadBatches: base.reload,
    openDeactivateDialog,
    handleDeactivate,
  };
}
