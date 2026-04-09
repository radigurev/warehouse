import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getGoodsReceiptById, completeGoodsReceipt, inspectReceiptLine, resolveQuarantinedLine } from '@features/purchasing/api/goods-receipts';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { GoodsReceiptDetailDto, InspectLineRequest, ResolveQuarantineRequest } from '@features/purchasing/types/purchasing';

export function useGoodsReceiptDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const receipt = ref<GoodsReceiptDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const receiptId = Number(route.params.id);

  onMounted(() => loadReceipt());

  async function loadReceipt(): Promise<void> {
    if (!receiptId || receiptId <= 0) {
      router.push({ name: 'goods-receipts' });
      return;
    }
    loading.value = true;
    try {
      receipt.value = await getGoodsReceiptById(receiptId);
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
    router.push({ name: 'goods-receipts' });
  }

  async function handleComplete(): Promise<void> {
    try {
      await completeGoodsReceipt(receiptId);
      notification.success(t('goodsReceipts.completed') + ' \u2713');
      await loadReceipt();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleInspect(lineId: number, request: InspectLineRequest): Promise<void> {
    try {
      await inspectReceiptLine(receiptId, lineId, request);
      notification.success(t('goodsReceipts.inspected') + ' \u2713');
      await loadReceipt();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleResolve(lineId: number, request: ResolveQuarantineRequest): Promise<void> {
    try {
      await resolveQuarantinedLine(receiptId, lineId, request);
      notification.success(t('goodsReceipts.resolved') + ' \u2713');
      await loadReceipt();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    receipt,
    loading,
    notFound,
    receiptId,
    loadReceipt,
    formatDate,
    goBack,
    handleComplete,
    handleInspect,
    handleResolve,
  };
}
