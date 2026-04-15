import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchGoodsReceipts } from '@features/purchasing/api/goods-receipts';
import type { GoodsReceiptDto, SearchGoodsReceiptsRequest } from '@features/purchasing/types/purchasing';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useGoodsReceiptsView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const receipts = ref<GoodsReceiptDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const showFormDialog = ref(false);
  const showDetailDialog = ref(false);
  const selectedReceipt = ref<GoodsReceiptDto | null>(null);

  const searchParams = ref<SearchGoodsReceiptsRequest>({
    sortDescending: true,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(receipts, ['receiptNumber', 'purchaseOrderNumber']);

  const filterPathMap: Record<string, string> = {
    receiptNumber: 'receiptNumber',
    purchaseOrderNumber: 'purchaseOrderNumber',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      page: 1,
    };
    loadReceipts();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('goodsReceipts.columns.receiptNumber'), key: 'receiptNumber', sortable: true },
    { title: t('goodsReceipts.columns.purchaseOrderNumber'), key: 'purchaseOrderNumber', sortable: true },
    { title: t('goodsReceipts.columns.warehouse'), key: 'warehouseId', sortable: false },
    { title: t('goodsReceipts.columns.receivedAt'), key: 'receivedAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadReceipts());

  async function loadReceipts(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchGoodsReceipts(searchParams.value);
      receipts.value = response.items;
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
      router.push({ name: 'goods-receipt-create' });
    } else {
      showFormDialog.value = true;
    }
  }

  function handleDetail(receipt: GoodsReceiptDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'goods-receipt-detail', params: { id: receipt.id } });
    } else {
      selectedReceipt.value = receipt;
      showDetailDialog.value = true;
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadReceipts();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadReceipts();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    showFormDialog,
    showDetailDialog,
    selectedReceipt,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadReceipts,
    formatDate,
    handleCreate,
    handleDetail,
    handlePageChange,
    handlePageSizeChange,
  };
}
