import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as authApi from '@/api/auth';
import type { LoginRequest } from '@/types/auth';

function parseJwtRoles(token: string): string[] {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload['roles'] || [];
    return Array.isArray(roles) ? roles : [roles];
  } catch {
    return [];
  }
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(localStorage.getItem('accessToken'));
  const refreshTokenValue = ref<string | null>(localStorage.getItem('refreshToken'));
  const expiresAt = ref<string | null>(localStorage.getItem('expiresAt'));
  const username = ref<string | null>(localStorage.getItem('username'));
  const roles = ref<string[]>(JSON.parse(localStorage.getItem('roles') || '[]'));

  const isAuthenticated = computed(() => {
    if (!accessToken.value || !expiresAt.value) {
      return false;
    }
    return new Date(expiresAt.value) > new Date();
  });

  const hasRefreshToken = computed(() => !!refreshTokenValue.value);
  const isAdmin = computed(() => roles.value.includes('Admin'));

  async function login(request: LoginRequest): Promise<void> {
    const response = await authApi.login(request);
    accessToken.value = response.accessToken;
    refreshTokenValue.value = response.refreshToken;
    expiresAt.value = response.expiresAt;
    username.value = request.username;
    roles.value = parseJwtRoles(response.accessToken);

    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('expiresAt', response.expiresAt);
    localStorage.setItem('username', request.username);
    localStorage.setItem('roles', JSON.stringify(roles.value));
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
    roles.value = [];

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('username');
    localStorage.removeItem('roles');
  }

  return {
    accessToken,
    refreshToken: refreshTokenValue,
    expiresAt,
    username,
    roles,
    isAuthenticated,
    hasRefreshToken,
    isAdmin,
    login,
    logout,
    clearState,
  };
});
