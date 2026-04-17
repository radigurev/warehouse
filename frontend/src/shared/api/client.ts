import axios from 'axios';
import type { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import { TokenRefreshManager } from '@shared/api/tokenRefreshManager';

const apiClient = axios.create({
  baseURL: '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});

const refreshManager = new TokenRefreshManager(apiClient);

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const accessToken = localStorage.getItem('accessToken');
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }

    if (!config.headers['X-Correlation-ID']) {
      try {
        config.headers['X-Correlation-ID'] = crypto.randomUUID();
      } catch {
        config.headers['X-Correlation-ID'] =
          'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
            const r = (Math.random() * 16) | 0;
            const v = c === 'x' ? r : (r & 0x3) | 0x8;
            return v.toString(16);
          });
      }
    }

    return config;
  },
);

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ProblemDetails>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken || originalRequest.url === '/auth/refresh' || originalRequest.url === '/auth/login') {
      return Promise.reject(error);
    }

    return refreshManager.handleUnauthorized(originalRequest);
  },
);

export default apiClient;
