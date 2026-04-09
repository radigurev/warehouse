import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchAdjustments } from '@features/inventory/api/adjustments';
import type { InventoryAdjustmentDto, SearchAdjustmentsRequest } from '@features/inventory/types/inventory';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useAdjustmentsView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const adjustments = ref<InventoryAdjustmentDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));

  const searchParams = ref<SearchAdjustmentsRequest>({
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(adjustments, [
    'reason',
    'status',
  ]);

  const filterPathMap: Record<string, string> = {
    reason: 'reason',
    status: 'status',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      filter: buildFilterString(columnFilters, filterPathMap),
      page: 1,
    };
    loadAdjustments();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('adjustments.columns.id'), key: 'id', sortable: true },
    { title: t('adjustments.columns.status'), key: 'status', sortable: true },
    { title: t('adjustments.columns.reason'), key: 'reason', sortable: true },
    { title: t('adjustments.columns.createdAt'), key: 'createdAtUtc', sortable: true },
    { title: t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '200px' },
  ]);

  onMounted(() => loadAdjustments());

  async function loadAdjustments(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchAdjustments(searchParams.value);
      adjustments.value = response.items;
      totalCount.value = response.totalCount;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
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

  const selectedAdjustment = ref<InventoryAdjustmentDto | null>(null);
  const showFormDialog = ref(false);
  const showDetailDialog = ref(false);

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'adjustment-create' });
    } else {
      showFormDialog.value = true;
    }
  }

  function handleDetail(adjustment: InventoryAdjustmentDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'adjustment-detail', params: { id: adjustment.id } });
    } else {
      selectedAdjustment.value = adjustment;
      showDetailDialog.value = true;
    }
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadAdjustments();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadAdjustments();
  }

  return {
    t,
    layout,
    loading,
    totalCount,
    totalPages,
    searchParams,
    columnFilters,
    filteredItems,
    headers,
    selectedAdjustment,
    showFormDialog,
    showDetailDialog,
    loadAdjustments,
    formatDate,
    translateStatus,
    statusColor,
    handleCreate,
    handleDetail,
    handlePageChange,
    handlePageSizeChange,
  };
}
