import { ref, computed } from 'vue';
import { useListView } from '@shared/composables/useListView';
import { searchSalesOrders, confirmSalesOrder, cancelSalesOrder, completeSalesOrder } from '@features/fulfillment/api/sales-orders';
import type { SalesOrderDto, SearchSalesOrdersRequest } from '@features/fulfillment/types/fulfillment';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';

export function useSalesOrdersView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const base = useListView<SalesOrderDto, SearchSalesOrdersRequest>({
    fetchFn: searchSalesOrders,
    filterFields: ['orderNumber', 'customerName', 'status'],
    filterPathMap: { orderNumber: 'orderNumber', customerName: 'customerName', status: 'status' },
    defaultSort: { field: 'createdAtUtc', descending: true },
    defaultPageSize: 25,
    navigation: {
      createRoute: { name: 'sales-order-create' },
      editRoute: (o: SalesOrderDto) => ({ name: 'sales-order-edit', params: { id: o.id } }),
      detailRoute: (o: SalesOrderDto) => ({ name: 'sales-order-detail', params: { id: o.id } }),
    },
  });

  const selectedOrder = base.selectedItem;
  const showConfirmDialog = ref(false);
  const showCancelDialog = ref(false);
  const showCompleteDialog = ref(false);
  const confirming = ref(false);
  const cancelling = ref(false);
  const completing = ref(false);

  const headers = computed(() => [
    { title: t('salesOrders.columns.orderNumber'), key: 'orderNumber', sortable: true },
    { title: t('salesOrders.columns.customerName'), key: 'customerName', sortable: true },
    { title: t('salesOrders.columns.status'), key: 'status', sortable: true },
    { title: t('salesOrders.columns.warehouseName'), key: 'warehouseName', sortable: false },
    { title: t('salesOrders.columns.requestedShipDate'), key: 'requestedShipDate', sortable: true },
    { title: t('salesOrders.columns.totalAmount'), key: 'totalAmount', sortable: true, align: 'end' as const },
    { title: t('salesOrders.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '380px' },
  ]);

  function getStatusColor(status: string): string {
    switch (status) {
      case 'Draft': return 'grey';
      case 'Confirmed': return 'blue';
      case 'Picking': return 'amber';
      case 'Packed': return 'indigo';
      case 'Shipped': return 'teal';
      case 'Completed': return 'green';
      case 'Cancelled': return 'red';
      default: return 'grey';
    }
  }

  function canEdit(order: SalesOrderDto): boolean {
    return order.status === 'Draft';
  }

  function canConfirm(order: SalesOrderDto): boolean {
    return order.status === 'Draft';
  }

  function canCancel(order: SalesOrderDto): boolean {
    return order.status === 'Draft' || order.status === 'Confirmed';
  }

  function canComplete(order: SalesOrderDto): boolean {
    return order.status === 'Shipped';
  }

  function openConfirmDialog(order: SalesOrderDto): void {
    selectedOrder.value = order;
    showConfirmDialog.value = true;
  }

  function openCancelDialog(order: SalesOrderDto): void {
    selectedOrder.value = order;
    showCancelDialog.value = true;
  }

  function openCompleteDialog(order: SalesOrderDto): void {
    selectedOrder.value = order;
    showCompleteDialog.value = true;
  }

  async function handleConfirm(): Promise<void> {
    if (!selectedOrder.value) return;
    confirming.value = true;
    try {
      await confirmSalesOrder(selectedOrder.value.id);
      notification.success(t('salesOrders.confirmed') + ' \u2713');
      showConfirmDialog.value = false;
      await base.reload();
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
      await cancelSalesOrder(selectedOrder.value.id);
      notification.success(t('salesOrders.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      cancelling.value = false;
    }
  }

  async function handleComplete(): Promise<void> {
    if (!selectedOrder.value) return;
    completing.value = true;
    try {
      await completeSalesOrder(selectedOrder.value.id);
      notification.success(t('salesOrders.completed') + ' \u2713');
      showCompleteDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      completing.value = false;
    }
  }

  return {
    ...base,
    selectedOrder,
    showConfirmDialog,
    showCancelDialog,
    showCompleteDialog,
    confirming,
    cancelling,
    completing,
    headers,
    getStatusColor,
    canEdit,
    canConfirm,
    canCancel,
    canComplete,
    openConfirmDialog,
    openCancelDialog,
    openCompleteDialog,
    handleConfirm,
    handleCancel,
    handleComplete,
  };
}
