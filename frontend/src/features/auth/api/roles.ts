import apiClient from '@shared/api/client';
import type { PaginatedResponse } from '@shared/types/api';
import type { RoleDto, RoleDetailDto, CreateRoleRequest, UpdateRoleRequest, AssignPermissionsRequest } from '@features/auth/types/role';
import type { PermissionDto } from '@features/auth/types/permission';

export function getRoles(page = 1, pageSize = 25): Promise<PaginatedResponse<RoleDto>> {
  return apiClient
    .get<PaginatedResponse<RoleDto>>('/roles', { params: { page, pageSize } })
    .then((r) => r.data);
}

export function getAllRoles(): Promise<RoleDto[]> {
  return apiClient
    .get<PaginatedResponse<RoleDto>>('/roles', { params: { page: 1, pageSize: 100 } })
    .then((r) => r.data.items);
}

export function getRoleById(id: number): Promise<RoleDetailDto> {
  return apiClient.get<RoleDetailDto>(`/roles/${id}`).then((r) => r.data);
}

export function createRole(request: CreateRoleRequest): Promise<RoleDetailDto> {
  return apiClient.post<RoleDetailDto>('/roles', request).then((r) => r.data);
}

export function updateRole(id: number, request: UpdateRoleRequest): Promise<RoleDetailDto> {
  return apiClient.put<RoleDetailDto>(`/roles/${id}`, request).then((r) => r.data);
}

export function deleteRole(id: number): Promise<void> {
  return apiClient.delete(`/roles/${id}`);
}

export function getRolePermissions(id: number): Promise<PermissionDto[]> {
  return apiClient.get<PermissionDto[]>(`/roles/${id}/permissions`).then((r) => r.data);
}

export function assignPermissions(id: number, request: AssignPermissionsRequest): Promise<void> {
  return apiClient.post(`/roles/${id}/permissions`, request);
}

export function removePermission(roleId: number, permissionId: number): Promise<void> {
  return apiClient.delete(`/roles/${roleId}/permissions/${permissionId}`);
}
