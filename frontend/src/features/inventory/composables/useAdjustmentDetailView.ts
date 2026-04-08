import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import {
  getAdjustmentById,
  approveAdjustment,
  rejectAdjustment,
  applyAdjustment,
} from '@features/inventory/api/adjustments';
import type { InventoryAdjustmentDetailDto } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useAdjustmentDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const adjustment = ref<InventoryAdjustmentDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const adjustmentId = Number(route.params.id);

  const isPending = computed(() => adjustment.value?.status === 'Pending');
  const isApproved = computed(() => adjustment.value?.status === 'Approved');
  const isReadOnly = computed(() => {
    const status = adjustment.value?.status;
    return status === 'Applied' || status === 'Rejected';
  });

  onMounted(() => loadAdjustment());

  async function loadAdjustment(): Promise<void> {
    if (!adjustmentId || adjustmentId <= 0) {
      router.push({ name: 'adjustments' });
      return;
    }
    loading.value = true;
    try {
      adjustment.value = await getAdjustmentById(adjustmentId);
    } catch {
      notFound.value = true;
    } finally {
      loading.value = false;
    }
  }

  function formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '\u2014';
    return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  function translateStatus(status: string): string {
    const key = `adjustments.statuses.${status}`;
    const translated = t(key);
    return translated !== key ? translated : status;
  }

  function statusColor(status: string): string {
    switch (status) {
      case 'Pending': return 'warning';
      case 'Approved': return 'info';
      case 'Rejected': return 'error';
      case 'Applied': return 'success';
      default: return 'default';
    }
  }

  function varianceColor(variance: number): string {
    if (variance > 0) return 'success';
    if (variance < 0) return 'error';
    return '';
  }

  function goBack(): void {
    router.push({ name: 'adjustments' });
  }

  async function handleApprove(notes?: string): Promise<void> {
    try {
      adjustment.value = await approveAdjustment(adjustmentId, { notes });
      notification.success(t('adjustments.approve') + ' \u2713');
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleReject(rejectionReason: string): Promise<void> {
    try {
      adjustment.value = await rejectAdjustment(adjustmentId, { notes: rejectionReason });
      notification.success(t('adjustments.reject') + ' \u2713');
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleApply(): Promise<void> {
    try {
      adjustment.value = await applyAdjustment(adjustmentId);
      notification.success(t('adjustments.apply') + ' \u2713');
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    adjustment,
    loading,
    notFound,
    adjustmentId,
    isPending,
    isApproved,
    isReadOnly,
    loadAdjustment,
    formatDate,
    translateStatus,
    statusColor,
    varianceColor,
    goBack,
    handleApprove,
    handleReject,
    handleApply,
  };
}
