import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@shared/stores/auth';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import { getUserById } from '@features/auth/api/users';
import type { UserDetailDto } from '@features/auth/types/user';

export function useSettingsView() {
  const { t } = useI18n();
  const router = useRouter();
  const auth = useAuthStore();
  const layout = useLayoutStore();
  const notification = useNotificationStore();

  const profile = ref<UserDetailDto | null>(null);
  const loading = ref(false);
  const showProfileDialog = ref(false);
  const showPasswordDialog = ref(false);

  onMounted(() => loadProfile());

  async function loadProfile(): Promise<void> {
    if (!auth.userId) return;
    loading.value = true;
    try {
      profile.value = await getUserById(auth.userId);
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    } finally {
      loading.value = false;
    }
  }

  function handleEditProfile(): void {
    if (layout.isPageMode) {
      router.push({ name: 'settings-edit-profile' });
    } else {
      showProfileDialog.value = true;
    }
  }

  function handleChangePassword(): void {
    if (layout.isPageMode) {
      router.push({ name: 'settings-change-password' });
    } else {
      showPasswordDialog.value = true;
    }
  }

  function onPasswordChanged(): void {
    notification.success(t('users.changePassword') + ' \u2713');
  }

  return {
    t,
    auth,
    profile,
    loading,
    showProfileDialog,
    showPasswordDialog,
    loadProfile,
    handleEditProfile,
    handleChangePassword,
    onPasswordChanged,
  };
}
