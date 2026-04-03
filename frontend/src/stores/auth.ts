import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as authApi from '@/api/auth';
import type { LoginRequest } from '@/types/auth';

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(localStorage.getItem('accessToken'));
  const refreshTokenValue = ref<string | null>(localStorage.getItem('refreshToken'));
  const expiresAt = ref<string | null>(localStorage.getItem('expiresAt'));
  const username = ref<string | null>(localStorage.getItem('username'));

  const isAuthenticated = computed(() => {
    if (!accessToken.value || !expiresAt.value) {
      return false;
    }
    return new Date(expiresAt.value) > new Date();
  });

  const hasRefreshToken = computed(() => !!refreshTokenValue.value);

  async function login(request: LoginRequest): Promise<void> {
    const response = await authApi.login(request);
    accessToken.value = response.accessToken;
    refreshTokenValue.value = response.refreshToken;
    expiresAt.value = response.expiresAt;
    username.value = request.username;

    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('expiresAt', response.expiresAt);
    localStorage.setItem('username', request.username);
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

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('username');
  }

  return {
    accessToken,
    refreshToken: refreshTokenValue,
    expiresAt,
    username,
    isAuthenticated,
    hasRefreshToken,
    login,
    logout,
    clearState,
  };
});
