import apiClient from '@shared/api/client';
import type { PaginatedResponse } from '@shared/types/api';
import type { PermissionDto, CreatePermissionRequest } from '@features/auth/types/permission';

export function getPermissions(page = 1, pageSize = 25): Promise<PaginatedResponse<PermissionDto>> {
  return apiClient
    .get<PaginatedResponse<PermissionDto>>('/permissions', { params: { page, pageSize } })
    .then((r) => r.data);
}

export function getAllPermissions(): Promise<PermissionDto[]> {
  return apiClient
    .get<PaginatedResponse<PermissionDto>>('/permissions', { params: { page: 1, pageSize: 100 } })
    .then((r) => r.data.items);
}

export function createPermission(request: CreatePermissionRequest): Promise<PermissionDto> {
  return apiClient.post<PermissionDto>('/permissions', request).then((r) => r.data);
}
