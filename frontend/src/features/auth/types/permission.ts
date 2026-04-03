export interface PermissionDto {
  id: number;
  resource: string;
  action: string;
  description: string | null;
}

export interface CreatePermissionRequest {
  resource: string;
  action: string;
  description: string | null;
}
