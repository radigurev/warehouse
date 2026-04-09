import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getPurchaseOrderById, confirmPurchaseOrder, cancelPurchaseOrder, closePurchaseOrder } from '@features/purchasing/api/purchase-orders';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { PurchaseOrderDetailDto } from '@features/purchasing/types/purchasing';

export function usePurchaseOrderDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const po = ref<PurchaseOrderDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const poId = Number(route.params.id);

  onMounted(() => loadPurchaseOrder());

  async function loadPurchaseOrder(): Promise<void> {
    if (!poId || poId <= 0) {
      router.push({ name: 'purchase-orders' });
      return;
    }
    loading.value = true;
    try {
      po.value = await getPurchaseOrderById(poId);
    } catch {
      notFound.value = true;
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

  function goBack(): void {
    router.push({ name: 'purchase-orders' });
  }

  async function handleConfirm(): Promise<void> {
    try {
      await confirmPurchaseOrder(poId);
      notification.success(t('purchaseOrders.confirmed') + ' \u2713');
      await loadPurchaseOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCancel(): Promise<void> {
    try {
      await cancelPurchaseOrder(poId);
      notification.success(t('purchaseOrders.cancelled') + ' \u2713');
      await loadPurchaseOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleClose(): Promise<void> {
    try {
      await closePurchaseOrder(poId);
      notification.success(t('purchaseOrders.closed') + ' \u2713');
      await loadPurchaseOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    po,
    loading,
    notFound,
    poId,
    loadPurchaseOrder,
    formatDate,
    goBack,
    handleConfirm,
    handleCancel,
    handleClose,
  };
}
