import { ref, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { buildFilterString } from '@shared/utils/buildFilterString';
import { searchPurchaseEvents } from '@features/purchasing/api/purchase-events';
import type { PurchaseEventDto, SearchPurchaseEventsRequest } from '@features/purchasing/types/purchasing';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function usePurchaseEventsView() {
  const { t, locale } = useI18n();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const events = ref<PurchaseEventDto[]>([]);
  const loading = ref(false);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / (searchParams.value.pageSize || 25)));

  const searchParams = ref<SearchPurchaseEventsRequest>({
    page: 1,
    pageSize: 25,
  });

  const { columnFilters, filteredItems } = useColumnFilters(events, ['eventType', 'entityType']);

  const filterPathMap: Record<string, string> = {
    eventType: 'eventType',
    entityType: 'entityType',
  };

  watch(columnFilters, () => {
    searchParams.value = {
      ...searchParams.value,
      page: 1,
    };
    loadEvents();
  }, { deep: true });

  const headers = computed(() => [
    { title: t('purchaseEvents.columns.occurredAt'), key: 'occurredAtUtc', sortable: true },
    { title: t('purchaseEvents.columns.eventType'), key: 'eventType', sortable: true },
    { title: t('purchaseEvents.columns.entityType'), key: 'entityType', sortable: true },
    { title: t('purchaseEvents.columns.entityId'), key: 'entityId', sortable: false },
    { title: t('purchaseEvents.columns.userId'), key: 'userId', sortable: false },
    { title: t('purchaseEvents.columns.payload'), key: 'payload', sortable: false },
  ]);

  onMounted(() => loadEvents());

  async function loadEvents(): Promise<void> {
    loading.value = true;
    try {
      const response = await searchPurchaseEvents(searchParams.value);
      events.value = response.items;
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
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  function truncatePayload(payload: string | null, maxLength: number = 80): string {
    if (!payload) return '';
    return payload.length > maxLength ? payload.substring(0, maxLength) + '...' : payload;
  }

  function handlePageChange(newPage: number): void {
    searchParams.value = { ...searchParams.value, page: newPage };
    loadEvents();
  }

  function handlePageSizeChange(newSize: number): void {
    searchParams.value = { ...searchParams.value, pageSize: newSize, page: 1 };
    loadEvents();
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
    loadEvents,
    formatDate,
    truncatePayload,
    handlePageChange,
    handlePageSizeChange,
  };
}
