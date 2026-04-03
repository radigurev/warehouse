import apiClient from '@shared/api/client';
import type { PermissionDto, CreatePermissionRequest } from '@features/auth/types/permission';

export function getPermissions(): Promise<PermissionDto[]> {
  return apiClient.get<PermissionDto[]>('/permissions').then((r) => r.data);
}

export function createPermission(request: CreatePermissionRequest): Promise<PermissionDto> {
  return apiClient.post<PermissionDto>('/permissions', request).then((r) => r.data);
}
