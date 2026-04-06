import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@shared/stores/auth';
import { getUsers } from '@features/auth/api/users';
import { getRoles } from '@features/auth/api/roles';
import { getPermissions } from '@features/auth/api/permissions';

export function useDashboardView() {
  const { t } = useI18n();
  const auth = useAuthStore();

  const stats = ref({ users: 0, roles: 0, permissions: 0 });

  onMounted(async () => {
    try {
      const [usersResponse, rolesResponse, permissionsResponse] = await Promise.all([
        getUsers(),
        getRoles(),
        getPermissions(),
      ]);
      stats.value = {
        users: usersResponse.totalCount,
        roles: rolesResponse.totalCount,
        permissions: permissionsResponse.totalCount,
      };
    } catch {
      // Dashboard stats are non-critical
    }
  });

  return {
    t,
    auth,
    stats,
  };
}
