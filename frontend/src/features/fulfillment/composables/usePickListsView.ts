import { ref, computed } from 'vue';
import { useListView } from '@shared/composables/useListView';
import { searchPickLists, cancelPickList } from '@features/fulfillment/api/pick-lists';
import type { PickListDto, SearchPickListsRequest } from '@features/fulfillment/types/fulfillment';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { useNotificationStore } from '@shared/stores/notification';
import { useI18n } from 'vue-i18n';

export function usePickListsView() {
  const { t } = useI18n();
  const notification = useNotificationStore();

  const base = useListView<PickListDto, SearchPickListsRequest>({
    fetchFn: searchPickLists,
    filterFields: ['pickListNumber', 'salesOrderNumber', 'status'],
    filterPathMap: { pickListNumber: 'pickListNumber', salesOrderNumber: 'salesOrderNumber', status: 'status' },
    defaultSort: { field: 'createdAtUtc', descending: true },
    defaultPageSize: 25,
    navigation: {
      detailRoute: (p: PickListDto) => ({ name: 'pick-list-detail', params: { id: p.id } }),
    },
  });

  const selectedPickList = base.selectedItem;
  const showCancelDialog = ref(false);
  const cancelling = ref(false);

  const headers = computed(() => [
    { title: t('pickLists.columns.pickListNumber'), key: 'pickListNumber', sortable: true },
    { title: t('pickLists.columns.salesOrderNumber'), key: 'salesOrderNumber', sortable: true },
    { title: t('pickLists.columns.warehouseName'), key: 'warehouseName', sortable: false },
    { title: t('pickLists.columns.status'), key: 'status', sortable: true },
    { title: t('pickLists.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  function getStatusColor(status: string): string {
    switch (status) {
      case 'Pending': return 'amber';
      case 'Completed': return 'green';
      case 'Cancelled': return 'red';
      default: return 'grey';
    }
  }

  function openCancelDialog(pickList: PickListDto): void {
    selectedPickList.value = pickList;
    showCancelDialog.value = true;
  }

  async function handleCancel(): Promise<void> {
    if (!selectedPickList.value) return;
    cancelling.value = true;
    try {
      await cancelPickList(selectedPickList.value.id);
      notification.success(t('pickLists.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await base.reload();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      cancelling.value = false;
    }
  }

  return {
    ...base,
    selectedPickList,
    showCancelDialog,
    cancelling,
    headers,
    getStatusColor,
    openCancelDialog,
    handleCancel,
  };
}
