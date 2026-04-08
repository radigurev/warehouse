import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { getRoles, deleteRole } from '@features/auth/api/roles';
import type { RoleDto } from '@features/auth/types/role';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

export function useRolesView() {
  const { t } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const roles = ref<RoleDto[]>([]);
  const loading = ref(false);
  const selectedRole = ref<RoleDto | null>(null);
  const showFormDialog = ref(false);
  const showPermissionsDialog = ref(false);
  const showDeleteDialog = ref(false);
  const deleting = ref(false);
  const page = ref(1);
  const pageSize = ref(25);
  const totalCount = ref(0);
  const totalPages = ref(0);

  const { columnFilters, filteredItems } = useColumnFilters(roles, ['name']);

  const headers = computed(() => [
    { title: t('roles.columns.name'), key: 'name', sortable: true },
    { title: t('roles.columns.description'), key: 'description', sortable: false },
    { title: t('roles.columns.isSystem'), key: 'isSystem', sortable: true },
    { title: t('roles.columns.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
  ]);

  onMounted(() => loadRoles());

  async function loadRoles(): Promise<void> {
    loading.value = true;
    try {
      const response = await getRoles(page.value, pageSize.value);
      roles.value = response.items;
      totalCount.value = response.totalCount;
      totalPages.value = response.totalPages;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      loading.value = false;
    }
  }

  function handlePageChange(newPage: number): void {
    page.value = newPage;
    loadRoles();
  }

  function handlePageSizeChange(newSize: number): void {
    pageSize.value = newSize;
    page.value = 1;
    loadRoles();
  }

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'role-create' });
    } else {
      selectedRole.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(role: RoleDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'role-edit', params: { id: role.id } });
    } else {
      selectedRole.value = role;
      showFormDialog.value = true;
    }
  }

  function handlePermissions(role: RoleDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'role-permissions', params: { id: role.id } });
    } else {
      selectedRole.value = role;
      showPermissionsDialog.value = true;
    }
  }

  function openDeleteDialog(role: RoleDto): void {
    selectedRole.value = role;
    showDeleteDialog.value = true;
  }

  async function handleDelete(): Promise<void> {
    if (!selectedRole.value) return;
    deleting.value = true;
    try {
      await deleteRole(selectedRole.value.id);
      notification.success(t('roles.delete') + ' \u2713');
      showDeleteDialog.value = false;
      await loadRoles();
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    } finally {
      deleting.value = false;
    }
  }

  return {
    t,
    layout,
    loading,
    selectedRole,
    showFormDialog,
    showPermissionsDialog,
    showDeleteDialog,
    deleting,
    page,
    pageSize,
    totalCount,
    totalPages,
    columnFilters,
    filteredItems,
    headers,
    loadRoles,
    handlePageChange,
    handlePageSizeChange,
    handleCreate,
    handleEdit,
    handlePermissions,
    openDeleteDialog,
    handleDelete,
  };
}
