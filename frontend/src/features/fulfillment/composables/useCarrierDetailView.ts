import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import {
  getCarrierById,
  deactivateCarrier,
  createServiceLevel,
  updateServiceLevel,
  deleteServiceLevel,
} from '@features/fulfillment/api/carriers';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type {
  CarrierDetailDto,
  CarrierServiceLevelDto,
  CreateServiceLevelRequest,
  UpdateServiceLevelRequest,
} from '@features/fulfillment/types/fulfillment';

export function useCarrierDetailView() {
  const { t, locale } = useI18n();
  const route = useRoute();
  const router = useRouter();
  const notification = useNotificationStore();
  const layout = useLayoutStore();

  const carrier = ref<CarrierDetailDto | null>(null);
  const loading = ref(false);
  const notFound = ref(false);
  const carrierId = Number(route.params.id);

  const showServiceLevelDialog = ref(false);
  const selectedServiceLevel = ref<CarrierServiceLevelDto | null>(null);
  const showDeleteServiceLevelDialog = ref(false);
  const deletingServiceLevel = ref(false);

  onMounted(() => loadCarrier());

  async function loadCarrier(): Promise<void> {
    if (!carrierId || carrierId <= 0) {
      router.push({ name: 'carriers' });
      return;
    }
    loading.value = true;
    try {
      carrier.value = await getCarrierById(carrierId);
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
    router.push({ name: 'carriers' });
  }

  function openCreateServiceLevel(): void {
    selectedServiceLevel.value = null;
    showServiceLevelDialog.value = true;
  }

  function openEditServiceLevel(level: CarrierServiceLevelDto): void {
    selectedServiceLevel.value = level;
    showServiceLevelDialog.value = true;
  }

  function openDeleteServiceLevel(level: CarrierServiceLevelDto): void {
    selectedServiceLevel.value = level;
    showDeleteServiceLevelDialog.value = true;
  }

  async function handleCreateServiceLevel(request: CreateServiceLevelRequest): Promise<void> {
    try {
      await createServiceLevel(carrierId, request);
      notification.success(t('carriers.serviceLevelCreated') + ' \u2713');
      showServiceLevelDialog.value = false;
      await loadCarrier();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleEditServiceLevel(levelId: number, request: UpdateServiceLevelRequest): Promise<void> {
    try {
      await updateServiceLevel(carrierId, levelId, request);
      notification.success(t('carriers.serviceLevelUpdated') + ' \u2713');
      showServiceLevelDialog.value = false;
      selectedServiceLevel.value = null;
      await loadCarrier();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleDeactivate(): Promise<void> {
    try {
      await deactivateCarrier(carrierId);
      notification.success(t('carriers.deactivated') + ' \u2713');
      await loadCarrier();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  }

  async function handleDeleteServiceLevel(levelId?: number): Promise<void> {
    const targetId = levelId ?? selectedServiceLevel.value?.id;
    if (!targetId) return;
    deletingServiceLevel.value = true;
    try {
      await deleteServiceLevel(carrierId, targetId);
      notification.success(t('carriers.serviceLevelDeleted') + ' \u2713');
      showDeleteServiceLevelDialog.value = false;
      selectedServiceLevel.value = null;
      await loadCarrier();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deletingServiceLevel.value = false;
    }
  }

  return {
    t,
    layout,
    carrier,
    loading,
    notFound,
    carrierId,
    showServiceLevelDialog,
    selectedServiceLevel,
    showDeleteServiceLevelDialog,
    deletingServiceLevel,
    loadCarrier,
    formatDate,
    goBack,
    openCreateServiceLevel,
    openEditServiceLevel,
    openDeleteServiceLevel,
    handleDeactivate,
    handleCreateServiceLevel,
    handleEditServiceLevel,
    handleDeleteServiceLevel,
  };
}
