import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchSupplierReturns, confirmSupplierReturn, cancelSupplierReturn } from '@features/purchasing/api/supplier-returns';
import type { SupplierReturnDto, SearchSupplierReturnsRequest } from '@features/purchasing/types/purchasing';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useSupplierReturnsView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const returns = ref<SupplierReturnDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const selectedReturn = ref<SupplierReturnDto | null>(null);
  const showFormDialog = ref(false);
  const showDetailDialog = ref(false);
  const showConfirmDialog = ref(false);
  const showCancelDialog = ref(false);
  const confirming = ref(false);
  const cancelling = ref(false);

  const searchParams = ref<SearchSupplierReturnsRequest>({
    sortDescending: true,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(returns, ['returnNumber', 'supplierName']);

  const filterPathMap: Record<string, string> = {
    returnNumber: 'returnNumber',
    supplierName: 'supplierName',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      page: 1,
    };
    loadReturns();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('supplierReturns.columns.returnNumber'), key: 'returnNumber', sortable: true },
    { title: t('supplierReturns.columns.supplierName'), key: 'supplierName', sortable: true },
    { title: t('supplierReturns.columns.status'), key: 'status', sortable: true },
    { title: t('supplierReturns.columns.reason'), key: 'reason', sortable: false },
    { title: t('supplierReturns.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  onMounted(() => loadReturns());

  async function loadReturns(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchSupplierReturns(searchParams.value);
      returns.value = response.items;
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

  function getStatusColor(status: string): string {
    switch (status) {
      case 'Draft': return 'grey';
      case 'Confirmed': return 'blue';
      case 'Cancelled': return 'red';
      default: return 'grey';
    }
  }

  function canConfirm(returnItem: SupplierReturnDto): boolean {
    return returnItem.status === 'Draft';
  }

  function canCancel(returnItem: SupplierReturnDto): boolean {
    return returnItem.status === 'Draft';
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'supplier-return-create' });
    } else {
      selectedReturn.value = null;
      showFormDialog.value = true;
    }
  }

  function handleDetail(returnItem: SupplierReturnDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'supplier-return-detail', params: { id: returnItem.id } });
    } else {
      selectedReturn.value = returnItem;
      showDetailDialog.value = true;
    }
  }

  function openConfirmDialog(returnItem: SupplierReturnDto): void {
    selectedReturn.value = returnItem;
    showConfirmDialog.value = true;
  }

  function openCancelDialog(returnItem: SupplierReturnDto): void {
    selectedReturn.value = returnItem;
    showCancelDialog.value = true;
  }

  async function handleConfirm(): Promise<void> {
    if (!selectedReturn.value) return;
    confirming.value = true;
    try {
      await confirmSupplierReturn(selectedReturn.value.id);
      notification.success(t('supplierReturns.confirmed') + ' \u2713');
      showConfirmDialog.value = false;
      await loadReturns();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      confirming.value = false;
    }
  }

  async function handleCancel(): Promise<void> {
    if (!selectedReturn.value) return;
    cancelling.value = true;
    try {
      await cancelSupplierReturn(selectedReturn.value.id);
      notification.success(t('supplierReturns.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await loadReturns();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      cancelling.value = false;
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadReturns();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadReturns();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    selectedReturn,
    showFormDialog,
    showDetailDialog,
    showConfirmDialog,
    showCancelDialog,
    confirming,
    cancelling,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadReturns,
    formatDate,
    getStatusColor,
    canConfirm,
    canCancel,
    handleCreate,
    handleDetail,
    openConfirmDialog,
    openCancelDialog,
    handleConfirm,
    handleCancel,
    handlePageChange,
    handlePageSizeChange,
  };
}
