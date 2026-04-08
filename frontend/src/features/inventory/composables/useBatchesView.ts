import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchBatches, deactivateBatch } from '@features/inventory/api/batches';
import type { BatchDto, SearchBatchesRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useBatchesView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const batches = ref<BatchDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const selectedBatch = ref<BatchDto | null>(null);
  const showFormDialog = ref(false);
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const searchParams = ref<SearchBatchesRequest>({
    includeExpired: true,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(batches, [
    'batchNumber',
    'productName',
  ]);

  const filterPathMap: Record<string, string> = {
    batchNumber: 'batchNumber',
    productName: 'product.name',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadBatches();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('batches.columns.batchNumber'), key: 'batchNumber', sortable: true },
    { title: t('batches.columns.product'), key: 'productName', sortable: true },
    { title: t('batches.columns.expiryDate'), key: 'expiryDate', sortable: true },
    { title: t('batches.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('batches.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  onMounted(() => loadBatches());

  async function loadBatches(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchBatches(searchParams.value);
      batches.value = response.items;
      totalCount.value = response.totalCount;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      loading.value = false;
    }
  }

  function formatDate(dateStr: string | null): string {
    if (!dateStr) return '\u2014';
    return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'batch-create' });
    } else {
      selectedBatch.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(batch: BatchDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'batch-edit', params: { id: batch.id } });
    } else {
      selectedBatch.value = batch;
      showFormDialog.value = true;
    }
  }

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
      await loadBatches();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deactivating.value = false;
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadBatches();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadBatches();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    selectedBatch,
    showFormDialog,
    showDeactivateDialog,
    deactivating,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadBatches,
    formatDate,
    handleCreate,
    handleEdit,
    openDeactivateDialog,
    handleDeactivate,
    handlePageChange,
    handlePageSizeChange,
  };
}
