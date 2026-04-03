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
      permissions.value = await getPermissions();
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      loading.value = false;
    }
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
    columnFilters,
    filteredItems,
    headers,
    loadPermissions,
    handleCreate,
  };
}
