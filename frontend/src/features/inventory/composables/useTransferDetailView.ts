import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getTransferById, completeTransfer, cancelTransfer } from '@features/inventory/api/transfers';
import type { WarehouseTransferDetailDto } from '@features/inventory/types/inventory';

export function useTransferDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const transfer = ref<WarehouseTransferDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const transferId = Number(route.params.id);

  const canComplete = computed(() => {
    const status = transfer.value?.status;
    return status === 'Draft';
  });

  const canCancel = computed(() => {
    const status = transfer.value?.status;
    return status === 'Draft';
  });

  const isReadOnly = computed(() => {
    const status = transfer.value?.status;
    return status === 'Completed' || status === 'Cancelled';
  });

  onMounted(() => loadTransfer());

  async function loadTransfer(): Promise<void> {
    if (!transferId || transferId <= 0) {
      router.push({ name: 'transfers' });
      return;
    }
    loading.value = true;
    try {
      transfer.value = await getTransferById(transferId);
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
    router.push({ name: 'transfers' });
  }

  function statusColor(status: string): string {
    switch (status) {
      case 'Draft': return 'warning';
      case 'Completed': return 'success';
      case 'Cancelled': return 'error';
      default: return 'grey';
    }
  }

  async function handleComplete(): Promise<void> {
    try {
      transfer.value = await completeTransfer(transferId, {});
      notification.success(t('transfers.complete'));
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  async function handleCancel(): Promise<void> {
    try {
      transfer.value = await cancelTransfer(transferId);
      notification.success(t('transfers.cancel'));
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  return {
    t,
    layout,
    transfer,
    loading,
    notFound,
    transferId,
    canComplete,
    canCancel,
    isReadOnly,
    loadTransfer,
    formatDate,
    goBack,
    statusColor,
    handleComplete,
    handleCancel,
  };
}
