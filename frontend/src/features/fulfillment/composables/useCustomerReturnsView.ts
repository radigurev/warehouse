import { ref, computed } from 'vue';
import { useListView } from '@shared/composables/useListView';
import {
  searchCustomerReturns,
  confirmCustomerReturn,
  receiveCustomerReturn,
  closeCustomerReturn,
  cancelCustomerReturn,
} from '@features/fulfillment/api/customer-returns';
import type { CustomerReturnDto, SearchCustomerReturnsRequest } from '@features/fulfillment/types/fulfillment';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';

export function useCustomerReturnsView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const base = useListView<CustomerReturnDto, SearchCustomerReturnsRequest>({
    fetchFn: searchCustomerReturns,
    filterFields: ['returnNumber', 'customerName', 'status'],
    filterPathMap: { returnNumber: 'returnNumber', customerName: 'customerName', status: 'status' },
    defaultSort: { field: 'createdAtUtc', descending: true },
    defaultPageSize: 25,
    navigation: {
      createRoute: { name: 'customer-return-create' },
      detailRoute: (r: CustomerReturnDto) => ({ name: 'customer-return-detail', params: { id: r.id } }),
    },
  });

  const selectedReturn = base.selectedItem;
  const showConfirmDialog = ref(false);
  const showCancelDialog = ref(false);
  const showReceiveDialog = ref(false);
  const showCloseDialog = ref(false);
  const confirming = ref(false);
  const cancelling = ref(false);
  const receiving = ref(false);
  const closing = ref(false);

  const headers = computed(() => [
    { title: t('customerReturns.columns.returnNumber'), key: 'returnNumber', sortable: true },
    { title: t('customerReturns.columns.customerName'), key: 'customerName', sortable: true },
    { title: t('customerReturns.columns.status'), key: 'status', sortable: true },
    { title: t('customerReturns.columns.salesOrderNumber'), key: 'salesOrderNumber', sortable: false },
    { title: t('customerReturns.columns.reason'), key: 'reason', sortable: false },
    { title: t('customerReturns.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '380px' },
  ]);

  function getStatusColor(status: string): string {
    switch (status) {
      case 'Draft': return 'grey';
      case 'Confirmed': return 'blue';
      case 'Received': return 'teal';
      case 'Closed': return 'green';
      case 'Cancelled': return 'red';
      default: return 'grey';
    }
  }

  function canConfirm(returnItem: CustomerReturnDto): boolean {
    return returnItem.status === 'Draft';
  }

  function canCancel(returnItem: CustomerReturnDto): boolean {
    return returnItem.status === 'Draft' || returnItem.status === 'Confirmed';
  }

  function canReceive(returnItem: CustomerReturnDto): boolean {
    return returnItem.status === 'Confirmed';
  }

  function canClose(returnItem: CustomerReturnDto): boolean {
    return returnItem.status === 'Received';
  }

  function openConfirmDialog(returnItem: CustomerReturnDto): void {
    selectedReturn.value = returnItem;
    showConfirmDialog.value = true;
  }

  function openCancelDialog(returnItem: CustomerReturnDto): void {
    selectedReturn.value = returnItem;
    showCancelDialog.value = true;
  }

  function openReceiveDialog(returnItem: CustomerReturnDto): void {
    selectedReturn.value = returnItem;
    showReceiveDialog.value = true;
  }

  function openCloseDialog(returnItem: CustomerReturnDto): void {
    selectedReturn.value = returnItem;
    showCloseDialog.value = true;
  }

  async function handleConfirm(): Promise<void> {
    if (!selectedReturn.value) return;
    confirming.value = true;
    try {
      await confirmCustomerReturn(selectedReturn.value.id);
      notification.success(t('customerReturns.confirmed') + ' \u2713');
      showConfirmDialog.value = false;
      await base.reload();
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
      await cancelCustomerReturn(selectedReturn.value.id);
      notification.success(t('customerReturns.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      cancelling.value = false;
    }
  }

  async function handleReceive(): Promise<void> {
    if (!selectedReturn.value) return;
    receiving.value = true;
    try {
      await receiveCustomerReturn(selectedReturn.value.id);
      notification.success(t('customerReturns.received') + ' \u2713');
      showReceiveDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      receiving.value = false;
    }
  }

  async function handleClose(): Promise<void> {
    if (!selectedReturn.value) return;
    closing.value = true;
    try {
      await closeCustomerReturn(selectedReturn.value.id);
      notification.success(t('customerReturns.closed') + ' \u2713');
      showCloseDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      closing.value = false;
    }
  }

  return {
    ...base,
    selectedReturn,
    showConfirmDialog,
    showCancelDialog,
    showReceiveDialog,
    showCloseDialog,
    confirming,
    cancelling,
    receiving,
    closing,
    headers,
    getStatusColor,
    canConfirm,
    canCancel,
    canReceive,
    canClose,
    openConfirmDialog,
    openCancelDialog,
    openReceiveDialog,
    openCloseDialog,
    handleConfirm,
    handleCancel,
    handleReceive,
    handleClose,
  };
}
