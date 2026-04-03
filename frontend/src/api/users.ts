import apiClient from './client';
import type { UserDto, UserDetailDto, CreateUserRequest, UpdateUserRequest, ChangePasswordRequest, AssignRolesRequest } from '@/types/user';
import type { RoleDto } from '@/types/role';
import type { PaginatedResponse } from '@/types/api';

export function getUsers(page: number = 1, pageSize: number = 25, includeInactive: boolean = false): Promise<PaginatedResponse<UserDto>> {
  return apiClient.get<PaginatedResponse<UserDto>>('/users', { params: { page, pageSize, includeInactive } }).then((r) => r.data);
}

export function getUserById(id: number): Promise<UserDetailDto> {
  return apiClient.get<UserDetailDto>(`/users/${id}`).then((r) => r.data);
}

export function createUser(request: CreateUserRequest): Promise<UserDetailDto> {
  return apiClient.post<UserDetailDto>('/users', request).then((r) => r.data);
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

export function getUserRoles(id: number): Promise<RoleDto[]> {
  return apiClient.get<RoleDto[]>(`/users/${id}/roles`).then((r) => r.data);
}

export function assignRoles(id: number, request: AssignRolesRequest): Promise<void> {
  return apiClient.post(`/users/${id}/roles`, request);
}

export function removeRole(userId: number, roleId: number): Promise<void> {
  return apiClient.delete(`/users/${userId}/roles/${roleId}`);
}
