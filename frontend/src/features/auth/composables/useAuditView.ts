import { ref, reactive, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { getAuditLogs } from '@features/auth/api/audit';
import { getUsers } from '@features/auth/api/users';
import type { AuditLogDto } from '@features/auth/types/audit';

export function useAuditView() {
  const { t, locale } = useI18n();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const auditLogs = ref<AuditLogDto[]>([]);
  const userMap = ref<Map<number, string>>(new Map());
  const loading = ref(false);
  const showDetailsDialog = ref(false);
  const dateFromMenu = ref(false);
  const dateToMenu = ref(false);
  const detailsJson = ref('');

  const { columnFilters, filteredItems } = useColumnFilters(auditLogs, ['action', 'resource']);

  const userItems = computed(() =>
    Array.from(userMap.value.entries()).map(([id, username]) => ({
      title: username,
      value: id,
    })),
  );

  const actionItems = computed(() => {
    const unique = new Set(auditLogs.value.map((log) => log.action));
    return Array.from(unique).sort();
  });

  function minLengthFilter(itemText: string, queryText: string): boolean {
    if (queryText.length < 2) return false;
    return itemText.toLowerCase().includes(queryText.toLowerCase());
  }

  const filters = reactive({
    userId: null as number | null,
    action: '',
    dateFrom: '',
    dateTo: '',
  });

  const headers = computed(() => [
    { title: t('audit.columns.dateTime'), key: 'createdAt', sortable: true },
    { title: t('audit.columns.user'), key: 'userId', sortable: true },
    { title: t('audit.columns.action'), key: 'action', sortable: true },
    { title: t('audit.columns.resource'), key: 'resource', sortable: true },
    { title: t('audit.columns.details'), key: 'details', sortable: false },
    { title: t('audit.columns.ipAddress'), key: 'ipAddress', sortable: false },
  ]);

  const formattedDetails = computed(() => {
    try {
      return JSON.stringify(JSON.parse(detailsJson.value), null, 2);
    } catch {
      return detailsJson.value;
    }
  });

  onMounted(async () => {
    await loadUsers();
    await loadAuditLogs();
  });

  async function loadUsers(): Promise<void> {
    try {
      const response = await getUsers(1, 1000, true);
      for (const user of response.items) {
        userMap.value.set(user.id, user.username);
      }
    } catch {
      // Non-critical -- will fall back to showing IDs
    }
  }

  async function loadAuditLogs(): Promise<void> {
    loading.value = true;
    try {
      const response = await getAuditLogs({
        userId: filters.userId || undefined,
        action: filters.action || undefined,
        fromDate: filters.dateFrom || undefined,
        toDate: filters.dateTo || undefined,
      });
      auditLogs.value = response.items;
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      loading.value = false;
    }
  }

  function formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  function formatDateValue(date: unknown): string {
    if (date instanceof Date) {
      return date.toISOString().split('T')[0];
    }
    return '';
  }

  function onDateFromPicked(value: unknown): void {
    filters.dateFrom = formatDateValue(value);
    dateFromMenu.value = false;
  }

  function onDateToPicked(value: unknown): void {
    filters.dateTo = formatDateValue(value);
    dateToMenu.value = false;
  }

  function clearFilters(): void {
    filters.userId = null;
    filters.action = '';
    filters.dateFrom = '';
    filters.dateTo = '';
    loadAuditLogs();
  }

  function openDetails(details: string): void {
    detailsJson.value = details;
    showDetailsDialog.value = true;
  }

  return {
    t,
    layout,
    loading,
    showDetailsDialog,
    dateFromMenu,
    dateToMenu,
    userMap,
    userItems,
    actionItems,
    minLengthFilter,
    filters,
    columnFilters,
    filteredItems,
    headers,
    formattedDetails,
    loadAuditLogs,
    formatDateTime,
    onDateFromPicked,
    onDateToPicked,
    clearFilters,
    openDetails,
  };
}
