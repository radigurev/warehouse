import type { RoleDto } from './role';

export interface UserDto {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
}

export interface UserDetailDto {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  roles: RoleDto[];
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AssignRolesRequest {
  roleIds: number[];
}
