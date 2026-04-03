import apiClient from '@shared/api/client';
import type { LoginRequest, LoginResponse, RefreshTokenRequest, RefreshTokenResponse } from '@features/auth/types/auth';

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>('/auth/login', request).then((r) => r.data);
}

export function refreshToken(request: RefreshTokenRequest): Promise<RefreshTokenResponse> {
  return apiClient.post<RefreshTokenResponse>('/auth/refresh', request).then((r) => r.data);
}

export function logout(refreshToken: string): Promise<void> {
  return apiClient.post('/auth/logout', { refreshToken });
}
