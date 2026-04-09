import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as authApi from '@features/auth/api/auth';
import { getUserPermissions } from '@features/auth/api/users';
import type { LoginRequest } from '@features/auth/types/auth';

function parseJwtPayload(token: string): Record<string, unknown> {
  try {
    return JSON.parse(atob(token.split('.')[1]));
  } catch {
    return {};
  }
}

function parseJwtUserId(token: string): number | null {
  const payload = parseJwtPayload(token);
  const sub = payload['sub'];
  const id = Number(sub);
  return id > 0 ? id : null;
}

function parseJwtUsername(token: string): string | null {
  const payload = parseJwtPayload(token);
  return (payload['username'] as string) ?? null;
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(localStorage.getItem('accessToken'));
  const refreshTokenValue = ref<string | null>(localStorage.getItem('refreshToken'));
  const expiresAt = ref<string | null>(localStorage.getItem('expiresAt'));
  const username = ref<string | null>(localStorage.getItem('username'));
  const userId = ref<number | null>(Number(localStorage.getItem('userId')) || null);
  const permissions = ref<string[]>(JSON.parse(localStorage.getItem('permissions') || '[]'));

  const isAuthenticated = computed(() => {
    if (!accessToken.value || !expiresAt.value) {
      return false;
    }
    return new Date(expiresAt.value) > new Date();
  });

  const hasRefreshToken = computed(() => !!refreshTokenValue.value);

  function hasPermission(permission: string): boolean {
    const resource = permission.split(':')[0];
    return permissions.value.includes(permission) || permissions.value.includes(`${resource}:all`);
  }

  const isAdmin = computed(() => hasPermission('users:all') || hasPermission('roles:all'));

  async function fetchPermissions(uid: number): Promise<void> {
    try {
      const response = await getUserPermissions(uid);
      permissions.value = response.permissions;
      localStorage.setItem('permissions', JSON.stringify(response.permissions));
    } catch {
      permissions.value = [];
      localStorage.removeItem('permissions');
    }
  }

  async function login(request: LoginRequest): Promise<void> {
    const response = await authApi.login(request);
    accessToken.value = response.accessToken;
    refreshTokenValue.value = response.refreshToken;
    expiresAt.value = response.expiresAt;
    userId.value = parseJwtUserId(response.accessToken);
    username.value = parseJwtUsername(response.accessToken) ?? request.username;

    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('expiresAt', response.expiresAt);
    localStorage.setItem('username', username.value ?? '');
    localStorage.setItem('userId', String(userId.value ?? ''));

    if (userId.value) {
      await fetchPermissions(userId.value);
    }
  }

  async function logout(): Promise<void> {
    if (refreshTokenValue.value) {
      try {
        await authApi.logout(refreshTokenValue.value);
      } catch {
        // Logout is best-effort — clear state regardless
      }
    }
    clearState();
  }

  function clearState(): void {
    accessToken.value = null;
    refreshTokenValue.value = null;
    expiresAt.value = null;
    username.value = null;
    userId.value = null;
    permissions.value = [];

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('username');
    localStorage.removeItem('userId');
    localStorage.removeItem('permissions');
    localStorage.removeItem('roles');
  }

  return {
    accessToken,
    refreshToken: refreshTokenValue,
    expiresAt,
    username,
    userId,
    permissions,
    isAuthenticated,
    hasRefreshToken,
    isAdmin,
    hasPermission,
    fetchPermissions,
    login,
    logout,
    clearState,
  };
});
