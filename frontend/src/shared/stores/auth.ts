import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as authApi from '@features/auth/api/auth';
import type { LoginRequest } from '@features/auth/types/auth';

function parseJwtPayload(token: string): Record<string, unknown> {
  try {
    return JSON.parse(atob(token.split('.')[1]));
  } catch {
    return {};
  }
}

function parseJwtRoles(token: string): string[] {
  const payload = parseJwtPayload(token);
  const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload['roles'] || [];
  return Array.isArray(roles) ? (roles as string[]) : [roles as string];
}

function parseJwtUserId(token: string): number | null {
  const payload = parseJwtPayload(token);
  const sub = payload['sub'];
  const id = Number(sub);
  return id > 0 ? id : null;
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(localStorage.getItem('accessToken'));
  const refreshTokenValue = ref<string | null>(localStorage.getItem('refreshToken'));
  const expiresAt = ref<string | null>(localStorage.getItem('expiresAt'));
  const username = ref<string | null>(localStorage.getItem('username'));
  const userId = ref<number | null>(Number(localStorage.getItem('userId')) || null);
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
    userId.value = parseJwtUserId(response.accessToken);
    roles.value = parseJwtRoles(response.accessToken);

    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('expiresAt', response.expiresAt);
    localStorage.setItem('username', request.username);
    localStorage.setItem('userId', String(userId.value ?? ''));
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
    userId.value = null;
    roles.value = [];

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('username');
    localStorage.removeItem('userId');
    localStorage.removeItem('roles');
  }

  return {
    accessToken,
    refreshToken: refreshTokenValue,
    expiresAt,
    username,
    userId,
    roles,
    isAuthenticated,
    hasRefreshToken,
    isAdmin,
    login,
    logout,
    clearState,
  };
});
