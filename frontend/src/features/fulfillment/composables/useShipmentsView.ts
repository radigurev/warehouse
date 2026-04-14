import { ref, computed } from 'vue';
import { useListView } from '@shared/composables/useListView';
import { searchShipments, updateShipmentStatus } from '@features/fulfillment/api/shipments';
import { useNotificationStore } from '@shared/stores/notification';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { ShipmentDto, SearchShipmentsRequest, UpdateShipmentStatusRequest } from '@features/fulfillment/types/fulfillment';
import { useI18n } from 'vue-i18n';

export function useShipmentsView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const showStatusDialog = ref(false);
  const selectedShipment = ref<ShipmentDto | null>(null);

  const base = useListView<ShipmentDto, SearchShipmentsRequest>({
    fetchFn: searchShipments,
    filterFields: ['shipmentNumber', 'salesOrderNumber', 'status'],
    filterPathMap: { shipmentNumber: 'shipmentNumber', salesOrderNumber: 'salesOrderNumber', status: 'status' },
    defaultSort: { field: 'dispatchedAtUtc', descending: true },
    defaultPageSize: 25,
    navigation: {
      detailRoute: (s: ShipmentDto) => ({ name: 'shipment-detail', params: { id: s.id } }),
    },
  });

  const headers = computed(() => [
    { title: t('shipments.columns.shipmentNumber'), key: 'shipmentNumber', sortable: true },
    { title: t('shipments.columns.salesOrderNumber'), key: 'salesOrderNumber', sortable: true },
    { title: t('shipments.columns.carrierName'), key: 'carrierName', sortable: false },
    { title: t('shipments.columns.status'), key: 'status', sortable: true },
    { title: t('shipments.columns.dispatchedAt'), key: 'dispatchedAtUtc', sortable: true },
    { title: t('shipments.columns.trackingNumber'), key: 'trackingNumber', sortable: false },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  function getStatusColor(status: string): string {
    switch (status) {
      case 'Dispatched': return 'blue';
      case 'InTransit': return 'amber';
      case 'OutForDelivery': return 'indigo';
      case 'Delivered': return 'green';
      case 'Failed': return 'red';
      case 'Returned': return 'grey-darken-2';
      default: return 'grey';
    }
  }

  function isTerminalStatus(status: string): boolean {
    return status === 'Delivered' || status === 'Returned';
  }

  function openStatusDialog(item: ShipmentDto): void {
    selectedShipment.value = item;
    showStatusDialog.value = true;
  }

  async function handleStatusUpdated(request: UpdateShipmentStatusRequest): Promise<void> {
    if (!selectedShipment.value) return;
    try {
      await updateShipmentStatus(selectedShipment.value.id, request);
      notification.success(t('shipments.statusUpdated') + ' \u2713');
      showStatusDialog.value = false;
      selectedShipment.value = null;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    ...base,
    headers,
    getStatusColor,
    isTerminalStatus,
    showStatusDialog,
    selectedShipment,
    openStatusDialog,
    handleStatusUpdated,
  };
}
