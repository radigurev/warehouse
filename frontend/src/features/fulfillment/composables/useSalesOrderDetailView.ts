import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getSalesOrderById, confirmSalesOrder, cancelSalesOrder, completeSalesOrder } from '@features/fulfillment/api/sales-orders';
import { generatePickList, getPickListById } from '@features/fulfillment/api/pick-lists';
import { createShipment } from '@features/fulfillment/api/shipments';
import { deleteParcel, removeParcelItem } from '@features/fulfillment/api/parcels';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { SalesOrderDetailDto, CreateShipmentRequest, PickListLineDto } from '@features/fulfillment/types/fulfillment';

export function useSalesOrderDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const so = ref<SalesOrderDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const activeTab = ref<string>('lines');
  const soId = Number(route.params.id);

  const showConfirmDialog = ref(false);
  const showCancelDialog = ref(false);
  const showCompleteDialog = ref(false);
  const showGeneratePickListDialog = ref(false);
  const showDispatchDialog = ref(false);

  onMounted(() => loadSalesOrder());

  async function loadSalesOrder(): Promise<void> {
    if (!soId || soId <= 0) {
      router.push({ name: 'sales-orders' });
      return;
    }
    loading.value = true;
    try {
      so.value = await getSalesOrderById(soId);
      await loadConfirmedPickLines();
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
    router.push({ name: 'sales-orders' });
  }

  async function handleConfirm(): Promise<void> {
    try {
      await confirmSalesOrder(soId);
      notification.success(t('salesOrders.confirmed') + ' \u2713');
      showConfirmDialog.value = false;
      await loadSalesOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCancel(): Promise<void> {
    try {
      await cancelSalesOrder(soId);
      notification.success(t('salesOrders.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await loadSalesOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleComplete(): Promise<void> {
    try {
      await completeSalesOrder(soId);
      notification.success(t('salesOrders.completed') + ' \u2713');
      showCompleteDialog.value = false;
      await loadSalesOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleGeneratePickList(): Promise<void> {
    try {
      const pickList = await generatePickList({ salesOrderId: soId });
      notification.success(t('salesOrders.pickListGenerated') + ' \u2713');
      showGeneratePickListDialog.value = false;
      await loadSalesOrder();
      router.push({ name: 'pick-list-detail', params: { id: pickList.id } });
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleDispatch(request: CreateShipmentRequest): Promise<void> {
    try {
      const shipment = await createShipment(request);
      notification.success(t('salesOrders.dispatched') + ' \u2713');
      showDispatchDialog.value = false;
      await loadSalesOrder();
      router.push({ name: 'shipment-detail', params: { id: shipment.id } });
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  function navigateToPickList(pickListId: number): void {
    router.push({ name: 'pick-list-detail', params: { id: pickListId } });
  }

  function navigateToShipment(shipmentId: number): void {
    router.push({ name: 'shipment-detail', params: { id: shipmentId } });
  }

  async function handleDeleteParcel(parcelId: number): Promise<void> {
    try {
      await deleteParcel(soId, parcelId);
      notification.success(t('salesOrders.parcelDeleted') + ' \u2713');
      await loadSalesOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleRemoveParcelItem(parcelId: number, itemId: number): Promise<void> {
    try {
      await removeParcelItem(soId, parcelId, itemId);
      notification.success(t('salesOrders.parcelItemRemoved') + ' \u2713');
      await loadSalesOrder();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  const confirmedPickLines = ref<PickListLineDto[]>([]);

  async function loadConfirmedPickLines(): Promise<void> {
    if (!so.value) {
      confirmedPickLines.value = [];
      return;
    }
    const completedSummaries = so.value.pickLists.filter((pl) => pl.status === 'Completed');
    if (completedSummaries.length === 0) {
      confirmedPickLines.value = [];
      return;
    }
    try {
      const details = await Promise.all(completedSummaries.map((pl) => getPickListById(pl.id)));
      confirmedPickLines.value = details.flatMap((d) => d.lines.filter((l) => l.status === 'Picked'));
    } catch {
      confirmedPickLines.value = [];
    }
  }

  return {
    t,
    layout,
    so,
    order: so,
    loading,
    notFound,
    activeTab,
    soId,
    orderId: soId,
    confirmedPickLines,
    showConfirmDialog,
    showCancelDialog,
    showCompleteDialog,
    showGeneratePickListDialog,
    showDispatchDialog,
    loadSalesOrder,
    formatDate,
    formatDateTime,
    goBack,
    handleConfirm,
    handleCancel,
    handleComplete,
    handleGeneratePickList,
    handleDispatch,
    navigateToPickList,
    navigateToShipment,
    handleDeleteParcel,
    handleRemoveParcelItem,
  };
}
