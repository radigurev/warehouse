import apiClient from '@shared/api/client';
import type { UserDto, UserDetailDto, CreateUserRequest, CreateUserResponse, UpdateUserRequest, ChangePasswordRequest, AssignRolesRequest, UserPermissionsResponse } from '@features/auth/types/user';
import type { RoleDto } from '@features/auth/types/role';
import type { PaginatedResponse } from '@shared/types/api';

export function getUsers(page: number = 1, pageSize: number = 25, includeInactive: boolean = false): Promise<PaginatedResponse<UserDto>> {
  return apiClient.get<PaginatedResponse<UserDto>>('/users', { params: { page, pageSize, includeInactive } }).then((r) => r.data);
}

export function getUserById(id: number): Promise<UserDetailDto> {
  return apiClient.get<UserDetailDto>(`/users/${id}`).then((r) => r.data);
}

export function createUser(request: CreateUserRequest): Promise<CreateUserResponse> {
  return apiClient.post<CreateUserResponse>('/users', request).then((r) => r.data);
}

export function updateUser(id: number, request: UpdateUserRequest): Promise<UserDetailDto> {
  return apiClient.put<UserDetailDto>(`/users/${id}`, request).then((r) => r.data);
}

export function deactivateUser(id: number): Promise<void> {
  return apiClient.delete(`/users/${id}`);
}

export function changePassword(id: number, request: ChangePasswordRequest): Promise<void> {
  return apiClient.put(`/users/${id}/password`, request);
}

export function resetPassword(id: number): Promise<CreateUserResponse> {
  return apiClient.post<CreateUserResponse>(`/users/${id}/reset-password`).then((r) => r.data);
}

export function getUserPermissions(id: number): Promise<UserPermissionsResponse> {
  return apiClient.get<UserPermissionsResponse>(`/users/${id}/permissions`).then((r) => r.data);
}

export function getUserRoles(id: number): Promise<RoleDto[]> {
  return apiClient.get<RoleDto[]>(`/users/${id}/roles`).then((r) => r.data);
}

export function assignRoles(id: number, request: AssignRolesRequest): Promise<void> {
  return apiClient.post(`/users/${id}/roles`, request);
}

export function removeRole(userId: number, roleId: number): Promise<void> {
  return apiClient.delete(`/users/${userId}/roles/${roleId}`);
}
