import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import type { RefreshTokenResponse } from '@features/auth/types/auth';

/**
 * Encapsulates token refresh logic: queue management, retry, and auth failure handling.
 * The Axios interceptor delegates to this manager instead of containing the logic inline.
 */
export class TokenRefreshManager {
  private isRefreshing = false;
  private failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (error: unknown) => void;
  }> = [];

  constructor(private readonly client: AxiosInstance) {}

  async handleUnauthorized(
    originalRequest: InternalAxiosRequestConfig & { _retry?: boolean },
  ): Promise<AxiosResponse> {
    if (this.isRefreshing) {
      return this.enqueueRequest(originalRequest);
    }

    this.isRefreshing = true;
    originalRequest._retry = true;

    try {
      const newToken = await this.attemptRefresh();
      this.processQueue(null, newToken);
      return this.retryRequest(originalRequest, newToken);
    } catch (error) {
      this.processQueue(error, null);
      this.handleAuthFailure();
      throw error;
    } finally {
      this.isRefreshing = false;
    }
  }

  private async attemptRefresh(): Promise<string> {
    const refreshToken = localStorage.getItem('refreshToken');
    const response = await this.client.post<RefreshTokenResponse>('/auth/refresh', {
      refreshToken,
    });

    const newAccessToken = response.data.accessToken;
    const newRefreshToken = response.data.refreshToken;
    const expiresAt = response.data.expiresAt;

    localStorage.setItem('accessToken', newAccessToken);
    localStorage.setItem('refreshToken', newRefreshToken);
    localStorage.setItem('expiresAt', expiresAt);

    await this.refreshPermissions(newAccessToken);

    return newAccessToken;
  }

  private async refreshPermissions(accessToken: string): Promise<void> {
    const userIdStr = localStorage.getItem('userId');
    if (!userIdStr) return;

    try {
      const response = await this.client.get(`/users/${userIdStr}/permissions`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      localStorage.setItem('permissions', JSON.stringify(response.data.permissions));
    } catch {
      // Permission refresh is best-effort during token refresh
    }
  }

  private processQueue(error: unknown, token: string | null): void {
    this.failedQueue.forEach((pending) => {
      if (token) {
        pending.resolve(token);
      } else {
        pending.reject(error);
      }
    });
    this.failedQueue = [];
  }

  private handleAuthFailure(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('user');
    window.location.href = '/login';
  }

  private enqueueRequest(
    originalRequest: InternalAxiosRequestConfig,
  ): Promise<AxiosResponse> {
    return new Promise<string>((resolve, reject) => {
      this.failedQueue.push({ resolve, reject });
    }).then((token) => {
      originalRequest.headers.Authorization = `Bearer ${token}`;
      return this.client(originalRequest);
    });
  }

  private retryRequest(
    originalRequest: InternalAxiosRequestConfig,
    token: string,
  ): Promise<AxiosResponse> {
    originalRequest.headers.Authorization = `Bearer ${token}`;
    return this.client(originalRequest);
  }
}
