import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import {
  getCustomerReturnById,
  confirmCustomerReturn,
  receiveCustomerReturn,
  closeCustomerReturn,
  cancelCustomerReturn,
} from '@features/fulfillment/api/customer-returns';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { CustomerReturnDetailDto } from '@features/fulfillment/types/fulfillment';

export function useCustomerReturnDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const customerReturn = ref<CustomerReturnDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const returnId = Number(route.params.id);

  const showConfirmDialog = ref(false);
  const showCancelDialog = ref(false);
  const showReceiveDialog = ref(false);
  const showCloseDialog = ref(false);

  onMounted(() => loadReturn());

  async function loadReturn(): Promise<void> {
    if (!returnId || returnId <= 0) {
      router.push({ name: 'customer-returns' });
      return;
    }
    loading.value = true;
    try {
      customerReturn.value = await getCustomerReturnById(returnId);
    } catch {
      notFound.value = true;
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

  function formatDateTime(dateStr: string | null): string {
    if (!dateStr) return '\u2014';
    return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  function goBack(): void {
    router.push({ name: 'customer-returns' });
  }

  function navigateToSalesOrder(): void {
    if (customerReturn.value?.salesOrderId) {
      router.push({ name: 'sales-order-detail', params: { id: customerReturn.value.salesOrderId } });
    }
  }

  async function handleConfirm(): Promise<void> {
    try {
      await confirmCustomerReturn(returnId);
      notification.success(t('customerReturns.confirmed') + ' \u2713');
      showConfirmDialog.value = false;
      await loadReturn();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCancel(): Promise<void> {
    try {
      await cancelCustomerReturn(returnId);
      notification.success(t('customerReturns.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await loadReturn();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleReceive(): Promise<void> {
    try {
      await receiveCustomerReturn(returnId);
      notification.success(t('customerReturns.received') + ' \u2713');
      showReceiveDialog.value = false;
      await loadReturn();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleClose(): Promise<void> {
    try {
      await closeCustomerReturn(returnId);
      notification.success(t('customerReturns.closed') + ' \u2713');
      showCloseDialog.value = false;
      await loadReturn();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    customerReturn,
    loading,
    notFound,
    returnId,
    showConfirmDialog,
    showCancelDialog,
    showReceiveDialog,
    showCloseDialog,
    loadReturn,
    formatDate,
    formatDateTime,
    goBack,
    navigateToSalesOrder,
    handleConfirm,
    handleCancel,
    handleReceive,
    handleClose,
  };
}
