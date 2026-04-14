import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getPickListById, pickLine, cancelPickList } from '@features/fulfillment/api/pick-lists';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { PickListDetailDto, PickListLineDto } from '@features/fulfillment/types/fulfillment';

export function usePickListDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const pickList = ref<PickListDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const pickListId = Number(route.params.id);

  const showPickDialog = ref(false);
  const showCancelDialog = ref(false);
  const selectedLine = ref<PickListLineDto | null>(null);

  onMounted(() => loadPickList());

  async function loadPickList(): Promise<void> {
    if (!pickListId || pickListId <= 0) {
      router.push({ name: 'pick-lists' });
      return;
    }
    loading.value = true;
    try {
      pickList.value = await getPickListById(pickListId);
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

  function goBack(): void {
    router.push({ name: 'pick-lists' });
  }

  function navigateToSalesOrder(): void {
    if (pickList.value?.salesOrderId) {
      router.push({ name: 'sales-order-detail', params: { id: pickList.value.salesOrderId } });
    }
  }

  function openPickDialog(line: PickListLineDto): void {
    selectedLine.value = line;
    showPickDialog.value = true;
  }

  async function handlePick(actualQuantity: number): Promise<void> {
    if (!selectedLine.value) return;
    try {
      await pickLine(pickListId, selectedLine.value.id, { actualQuantity });
      notification.success(t('pickLists.linePicked') + ' \u2713');
      showPickDialog.value = false;
      selectedLine.value = null;
      await loadPickList();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCancelPickList(): Promise<void> {
    try {
      await cancelPickList(pickListId);
      notification.success(t('pickLists.cancelled') + ' \u2713');
      showCancelDialog.value = false;
      await loadPickList();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    pickList,
    loading,
    notFound,
    pickListId,
    showPickDialog,
    showCancelDialog,
    selectedLine,
    loadPickList,
    formatDate,
    goBack,
    navigateToSalesOrder,
    openPickDialog,
    handlePick,
    handleCancelPickList,
  };
}
