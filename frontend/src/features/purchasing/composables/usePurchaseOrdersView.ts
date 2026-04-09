import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchPurchaseOrders, confirmPurchaseOrder, cancelPurchaseOrder, closePurchaseOrder } from '@features/purchasing/api/purchase-orders';
import type { PurchaseOrderDto, SearchPurchaseOrdersRequest } from '@features/purchasing/types/purchasing';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function usePurchaseOrdersView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const orders = ref<PurchaseOrderDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));
  const selectedOrder = ref<PurchaseOrderDto | null>(null);
  const showFormDialog = ref(false);
  const showDetailDialog = ref(false);
  const showConfirmDialog = ref(false);
  const showCancelDialog = ref(false);
  const showCloseDialog = ref(false);
  const confirming = ref(false);
  const cancelling = ref(false);
  const closing = ref(false);

  const searchParams = ref<SearchPurchaseOrdersRequest>({
    sortBy: 'createdAtUtc',
    sortDescending: true,
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(orders, ['orderNumber', 'supplierName', 'status']);

  const filterPathMap: Record<string, string> = {
    orderNumber: 'orderNumber',
    supplierName: 'supplierName',
    status: 'status',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      page: 1,
    };
    loadOrders();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('purchaseOrders.columns.orderNumber'), key: 'orderNumber', sortable: true },
    { title: t('purchaseOrders.columns.supplierName'), key: 'supplierName', sortable: true },
    { title: t('purchaseOrders.columns.status'), key: 'status', sortable: true },
    { title: t('purchaseOrders.columns.destinationWarehouse'), key: 'destinationWarehouseId', sortable: false },
    { title: t('purchaseOrders.columns.expectedDeliveryDate'), key: 'expectedDeliveryDate', sortable: true },
    { title: t('purchaseOrders.columns.totalAmount'), key: 'totalAmount', sortable: true, align: 'end' as const },
    { title: t('purchaseOrders.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '380px' },
  ]);

  onMounted(() => loadOrders());

  async function loadOrders(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchPurchaseOrders(searchParams.value);
      orders.value = response.items;
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
      case 'PartiallyReceived': return 'orange';
      case 'Received': return 'green';
      case 'Closed': return 'grey-darken-2';
      case 'Cancelled': return 'red';
      default: return 'grey';
    }
  }

  function canEdit(order: PurchaseOrderDto): boolean {
    return order.status === 'Draft';
  }

  function canConfirm(order: PurchaseOrderDto): boolean {
    return order.status === 'Draft';
  }

  function canCancel(order: PurchaseOrderDto): boolean {
    return order.status === 'Draft' || order.status === 'Confirmed';
  }

  function canClose(order: PurchaseOrderDto): boolean {
    return order.status === 'PartiallyReceived' || order.status === 'Received';
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'purchase-order-create' });
    } else {
      selectedOrder.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(order: PurchaseOrderDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'purchase-order-edit', params: { id: order.id } });
    } else {
      selectedOrder.value = order;
      showFormDialog.value = true;
    }
  }

  function handleDetail(order: PurchaseOrderDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'purchase-order-detail', params: { id: order.id } });
    } else {
      selectedOrder.value = order;
      showDetailDialog.value = true;
    }
  }

  function openConfirmDialog(order: PurchaseOrderDto): void {
    selectedOrder.value = order;
    showConfirmDialog.value = true;
  }

  function openCancelDialog(order: PurchaseOrderDto): void {
    selectedOrder.value = order;
    showCancelDialog.value = true;
  }

  function openCloseDialog(order: PurchaseOrderDto): void {
    selectedOrder.value = order;
    showCloseDialog.value = true;
  }

  async function handleConfirm(): Promise<void> {
    if (!selectedOrder.value) return;
    confirming.value = true;
    try {
      await confirmPurchaseOrder(selectedOrder.value.id);
      notification.success(t('purchaseOrders.confirmed') + ' \u2713');
      showConfirmDialog.value = false;
      await loadOrders();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      confirming.value = false;
    }
  }

  async function handleCancel(): Promise<void> {
    if (!selectedOrder.value) return;
    cancelling.value = true;
    try {
      await cancelPurchaseOrder(selectedOrder.value.id);
      notification.success(t('purchaseOrders.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await loadOrders();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      cancelling.value = false;
    }
  }

  async function handleClose(): Promise<void> {
    if (!selectedOrder.value) return;
    closing.value = true;
    try {
      await closePurchaseOrder(selectedOrder.value.id);
      notification.success(t('purchaseOrders.closed') + ' \u2713');
      showCloseDialog.value = false;
      await loadOrders();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      closing.value = false;
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadOrders();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadOrders();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    selectedOrder,
    showFormDialog,
    showDetailDialog,
    showConfirmDialog,
    showCancelDialog,
    showCloseDialog,
    confirming,
    cancelling,
    closing,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    loadOrders,
    formatDate,
    getStatusColor,
    canEdit,
    canConfirm,
    canCancel,
    canClose,
    handleCreate,
    handleEdit,
    handleDetail,
    openConfirmDialog,
    openCancelDialog,
    openCloseDialog,
    handleConfirm,
    handleCancel,
    handleClose,
    handlePageChange,
    handlePageSizeChange,
  };
}
