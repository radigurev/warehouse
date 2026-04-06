import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { getPermissions } from '@features/auth/api/permissions';
import type { PermissionDto } from '@features/auth/types/permission';

export function usePermissionsView() {
  const { t } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const permissions = ref<PermissionDto[]>([]);
  const loading = ref(false);
  const showFormDialog = ref(false);
  const page = ref(1);
  const pageSize = ref(25);
  const totalCount = ref(0);
  const totalPages = ref(0);

  const { columnFilters, filteredItems } = useColumnFilters(permissions, ['resource', 'action']);

  const headers = computed(() => [
    { title: t('permissions.columns.resource'), key: 'resource', sortable: true },
    { title: t('permissions.columns.action'), key: 'action', sortable: true },
    { title: t('permissions.columns.description'), key: 'description', sortable: false },
  ]);

  onMounted(() => loadPermissions());

  async function loadPermissions(): Promise<void> {
    loading.value = true;
    try {
      const response = await getPermissions(page.value, pageSize.value);
      permissions.value = response.items;
      totalCount.value = response.totalCount;
      totalPages.value = response.totalPages;
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      loading.value = false;
    }
  }

  function handlePageChange(newPage: number): void {
    page.value = newPage;
    loadPermissions();
  }

  function handlePageSizeChange(newSize: number): void {
    pageSize.value = newSize;
    page.value = 1;
    loadPermissions();
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'permission-create' });
    } else {
      showFormDialog.value = true;
    }
  }

  return {
    t,
    layout,
    loading,
    showFormDialog,
    page,
    pageSize,
    totalCount,
    totalPages,
    columnFilters,
    filteredItems,
    headers,
    loadPermissions,
    handlePageChange,
    handlePageSizeChange,
    handleCreate,
  };
}
