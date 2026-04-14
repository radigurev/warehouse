import { ref, computed } from 'vue';
import { useListView } from '@shared/composables/useListView';
import { searchCarriers, deactivateCarrier } from '@features/fulfillment/api/carriers';
import type { CarrierDto, SearchCarriersRequest } from '@features/fulfillment/types/fulfillment';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';

export function useCarriersView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const base = useListView<CarrierDto, SearchCarriersRequest>({
    fetchFn: searchCarriers,
    filterFields: ['name', 'code'],
    filterPathMap: { name: 'name', code: 'code' },
    defaultSort: { field: 'name', descending: false },
    defaultPageSize: 25,
    navigation: {
      createRoute: { name: 'carrier-create' },
      editRoute: (c: CarrierDto) => ({ name: 'carrier-edit', params: { id: c.id } }),
      detailRoute: (c: CarrierDto) => ({ name: 'carrier-detail', params: { id: c.id } }),
    },
  });

  const selectedCarrier = base.selectedItem;
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);

  const headers = computed(() => [
    { title: t('carriers.columns.code'), key: 'code', sortable: true },
    { title: t('carriers.columns.name'), key: 'name', sortable: true },
    { title: t('carriers.columns.contactEmail'), key: 'contactEmail', sortable: false },
    { title: t('carriers.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  function openDeactivateDialog(carrier: CarrierDto): void {
    selectedCarrier.value = carrier;
    showDeactivateDialog.value = true;
  }

  async function handleDeactivate(): Promise<void> {
    if (!selectedCarrier.value) return;
    deactivating.value = true;
    try {
      await deactivateCarrier(selectedCarrier.value.id);
      notification.success(t('carriers.deactivated') + ' \u2713');
      showDeactivateDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deactivating.value = false;
    }
  }

  return {
    ...base,
    selectedCarrier,
    showDeactivateDialog,
    deactivating,
    headers,
    openDeactivateDialog,
    handleDeactivate,
  };
}
