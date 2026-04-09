import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getSupplierReturnById, confirmSupplierReturn, cancelSupplierReturn } from '@features/purchasing/api/supplier-returns';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { SupplierReturnDetailDto } from '@features/purchasing/types/purchasing';

export function useSupplierReturnDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const supplierReturn = ref<SupplierReturnDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const returnId = Number(route.params.id);

  onMounted(() => loadReturn());

  async function loadReturn(): Promise<void> {
    if (!returnId || returnId <= 0) {
      router.push({ name: 'supplier-returns' });
      return;
    }
    loading.value = true;
    try {
      supplierReturn.value = await getSupplierReturnById(returnId);
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
    router.push({ name: 'supplier-returns' });
  }

  async function handleConfirm(): Promise<void> {
    try {
      await confirmSupplierReturn(returnId);
      notification.success(t('supplierReturns.confirmed') + ' \u2713');
      await loadReturn();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleCancel(): Promise<void> {
    try {
      await cancelSupplierReturn(returnId);
      notification.success(t('supplierReturns.cancelled') + ' \u2713');
      await loadReturn();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  return {
    t,
    layout,
    supplierReturn,
    loading,
    notFound,
    returnId,
    loadReturn,
    formatDate,
    goBack,
    handleConfirm,
    handleCancel,
  };
}
