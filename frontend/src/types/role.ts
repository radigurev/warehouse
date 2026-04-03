import type { PermissionDto } from './permission';

export interface RoleDto {
  id: number;
  name: string;
  description: string | null;
  isSystem: boolean;
}

export interface RoleDetailDto {
  id: number;
  name: string;
  description: string | null;
  isSystem: boolean;
  createdAt: string;
  permissions: PermissionDto[];
}

export interface CreateRoleRequest {
  name: string;
  description: string | null;
}

export interface UpdateRoleRequest {
  name: string;
  description: string | null;
}

export interface AssignPermissionsRequest {
  permissionIds: number[];
}
