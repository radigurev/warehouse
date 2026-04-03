import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@shared/stores/layout';
import { useAuthStore } from '@shared/stores/auth';
import { useNotificationStore } from '@shared/stores/notification';
import { useColumnFilters } from '@shared/composables/useColumnFilters';
import { getUsers, deactivateUser, resetPassword } from '@features/auth/api/users';
import type { UserDto } from '@features/auth/types/user';

export function useUsersView() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const layout = useLayoutStore();
  const auth = useAuthStore();
  const notification = useNotificationStore();

  const users = ref<UserDto[]>([]);
  const loading = ref(false);
  const selectedUser = ref<UserDto | null>(null);
  const showFormDialog = ref(false);
  const showRolesDialog = ref(false);
  const showDeactivateDialog = ref(false);
  const deactivating = ref(false);
  const showPasswordDialog = ref(false);
  const generatedPassword = ref('');

  const { columnFilters, filteredItems } = useColumnFilters(users, ['username', 'email', 'firstName', 'lastName']);

  const headers = computed(() => [
    { title: t('users.columns.username'), key: 'username', sortable: true },
    { title: t('users.columns.email'), key: 'email', sortable: true },
    { title: t('users.columns.firstName'), key: 'firstName', sortable: true },
    { title: t('users.columns.lastName'), key: 'lastName', sortable: true },
    { title: t('users.columns.isActive'), key: 'isActive', sortable: true },
    { title: t('users.columns.createdAt'), key: 'createdAt', sortable: true },
    { title: t('users.columns.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '340px' },
  ]);

  onMounted(() => loadUsers());

  async function loadUsers(): Promise<void> {
    loading.value = true;
    try {
      const response = await getUsers();
      users.value = response.items;
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
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

  function handleCreate(): void {
    if (layout.isPageMode) {
      router.push({ name: 'user-create' });
    } else {
      selectedUser.value = null;
      showFormDialog.value = true;
    }
  }

  function handleEdit(user: UserDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'user-edit', params: { id: user.id } });
    } else {
      selectedUser.value = user;
      showFormDialog.value = true;
    }
  }

  function handleRoles(user: UserDto): void {
    if (layout.isPageMode) {
      router.push({ name: 'user-roles', params: { id: user.id } });
    } else {
      selectedUser.value = user;
      showRolesDialog.value = true;
    }
  }

  async function openResetPasswordDialog(user: UserDto): Promise<void> {
    try {
      const response = await resetPassword(user.id);
      generatedPassword.value = response.generatedPassword;
      showPasswordDialog.value = true;
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  }

  function copyPassword(): void {
    navigator.clipboard.writeText(generatedPassword.value);
    notification.success(t('users.passwordCopied'));
  }

  function openDeactivateDialog(user: UserDto): void {
    if (user.username === auth.username) {
      notification.warning(t('users.deactivateSelfWarning'));
      return;
    }
    selectedUser.value = user;
    showDeactivateDialog.value = true;
  }

  async function handleDeactivate(): Promise<void> {
    if (!selectedUser.value) return;
    deactivating.value = true;
    try {
      await deactivateUser(selectedUser.value.id);
      notification.success(t('users.deactivate') + ' \u2713');
      showDeactivateDialog.value = false;
      await loadUsers();
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      deactivating.value = false;
    }
  }

  return {
    t,
    layout,
    loading,
    selectedUser,
    showFormDialog,
    showRolesDialog,
    showDeactivateDialog,
    deactivating,
    showPasswordDialog,
    generatedPassword,
    columnFilters,
    filteredItems,
    headers,
    loadUsers,
    formatDate,
    handleCreate,
    handleEdit,
    handleRoles,
    openResetPasswordDialog,
    copyPassword,
    openDeactivateDialog,
    handleDeactivate,
  };
}
