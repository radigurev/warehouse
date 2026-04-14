import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getShipmentById, updateShipmentStatus } from '@features/fulfillment/api/shipments';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { ShipmentDetailDto, ShipmentStatus, UpdateShipmentStatusRequest } from '@features/fulfillment/types/fulfillment';

const validTransitions: Record<string, ShipmentStatus[]> = {
  Dispatched: ['InTransit', 'Delivered', 'Failed'],
  InTransit: ['OutForDelivery', 'Delivered', 'Failed'],
  OutForDelivery: ['Delivered', 'Failed'],
  Failed: ['Returned'],
};

export function useShipmentDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const shipment = ref<ShipmentDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const shipmentId = Number(route.params.id);

  const showStatusUpdateDialog = ref(false);

  onMounted(() => loadShipment());

  async function loadShipment(): Promise<void> {
    if (!shipmentId || shipmentId <= 0) {
      router.push({ name: 'shipments' });
      return;
    }
    loading.value = true;
    try {
      shipment.value = await getShipmentById(shipmentId);
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
    router.push({ name: 'shipments' });
  }

  function navigateToSalesOrder(): void {
    if (shipment.value?.salesOrderId) {
      router.push({ name: 'sales-order-detail', params: { id: shipment.value.salesOrderId } });
    }
  }

  function getValidTransitions(currentStatus: string): ShipmentStatus[] {
    return validTransitions[currentStatus] ?? [];
  }

  function isTerminalStatus(status: string): boolean {
    return status === 'Delivered' || status === 'Returned';
  }

  async function handleStatusUpdate(request: UpdateShipmentStatusRequest): Promise<void> {
    try {
      await updateShipmentStatus(shipmentId, request);
      notification.success(t('shipments.statusUpdated') + ' \u2713');
      showStatusUpdateDialog.value = false;
      await loadShipment();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    shipment,
    loading,
    notFound,
    shipmentId,
    showStatusUpdateDialog,
    loadShipment,
    formatDate,
    formatDateTime,
    goBack,
    navigateToSalesOrder,
    getValidTransitions,
    isTerminalStatus,
    handleStatusUpdate,
  };
}
