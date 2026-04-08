import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import {
  getSessionById,
  startSession,
  completeSession,
  cancelSession,
  addCount,
  updateCount,
  deleteCount,
  getVarianceReport,
  createAdjustmentFromSession,
} from '@features/inventory/api/stocktake';
import type {
  StocktakeSessionDetailDto,
  StocktakeCountDto,
  RecordStocktakeCountRequest,
  UpdateStocktakeCountRequest,
} from '@features/inventory/types/inventory';

export function useStocktakeDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const session = ref<StocktakeSessionDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const varianceReport = ref<StocktakeCountDto[]>([]);
  const showVarianceReport = ref(false);
  const sessionId = Number(route.params.id);

  const isDraft = computed(() => session.value?.status === 'Draft');
  const isInProgress = computed(() => session.value?.status === 'InProgress');
  const isCompleted = computed(() => session.value?.status === 'Completed');
  const isCancelled = computed(() => session.value?.status === 'Cancelled');
  const isReadOnly = computed(() => isCompleted.value || isCancelled.value);

  onMounted(() => loadSession());

  async function loadSession(): Promise<void> {
    if (!sessionId || sessionId <= 0) {
      router.push({ name: 'stocktake' });
      return;
    }
    loading.value = true;
    try {
      session.value = await getSessionById(sessionId);
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
    const key = `stocktake.statuses.${status}`;
    const translated = t(key);
    return translated !== key ? translated : status;
  }

  function statusColor(status: string): string {
    switch (status) {
      case 'Draft': return 'default';
      case 'InProgress': return 'info';
      case 'Completed': return 'success';
      case 'Cancelled': return 'error';
      default: return 'default';
    }
  }

  function varianceColor(variance: number): string {
    if (variance > 0) return 'success';
    if (variance < 0) return 'error';
    return '';
  }

  function goBack(): void {
    router.push({ name: 'stocktake' });
  }

  async function handleStart(): Promise<void> {
    try {
      session.value = await startSession(sessionId);
      notification.success(t('stocktake.start') + ' \u2713');
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleComplete(): Promise<void> {
    try {
      session.value = await completeSession(sessionId);
      notification.success(t('stocktake.complete') + ' \u2713');
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCancel(): Promise<void> {
    try {
      session.value = await cancelSession(sessionId);
      notification.success(t('stocktake.cancelSession') + ' \u2713');
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleAddCount(request: RecordStocktakeCountRequest): Promise<void> {
    try {
      await addCount(sessionId, request);
      await loadSession();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleUpdateCount(countId: number, request: UpdateStocktakeCountRequest): Promise<void> {
    try {
      await updateCount(sessionId, countId, request);
      await loadSession();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleDeleteCount(countId: number): Promise<void> {
    try {
      await deleteCount(sessionId, countId);
      await loadSession();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleViewVarianceReport(): Promise<void> {
    try {
      varianceReport.value = await getVarianceReport(sessionId);
      showVarianceReport.value = true;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCreateAdjustment(): Promise<void> {
    try {
      const adj = await createAdjustmentFromSession(sessionId);
      notification.success(t('stocktake.createAdjustment') + ' \u2713');
      router.push({ name: 'adjustment-detail', params: { id: adj.id } });
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    session,
    loading,
    notFound,
    sessionId,
    varianceReport,
    showVarianceReport,
    isDraft,
    isInProgress,
    isCompleted,
    isCancelled,
    isReadOnly,
    loadSession,
    formatDate,
    translateStatus,
    statusColor,
    varianceColor,
    goBack,
    handleStart,
    handleComplete,
    handleCancel,
    handleAddCount,
    handleUpdateCount,
    handleDeleteCount,
    handleViewVarianceReport,
    handleCreateAdjustment,
  };
}
