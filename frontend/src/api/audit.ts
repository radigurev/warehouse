import apiClient from './client';
import type { AuditLogDto } from '@/types/audit';
import type { PaginatedResponse } from '@/types/api';

export interface AuditQueryParams {
  page?: number;
  pageSize?: number;
  userId?: number;
  action?: string;
  fromDate?: string;
  toDate?: string;
}

export function getAuditLogs(params: AuditQueryParams = {}): Promise<PaginatedResponse<AuditLogDto>> {
  return apiClient.get<PaginatedResponse<AuditLogDto>>('/audit', { params }).then((r) => r.data);
}

export function getAuditLogsByUser(userId: number, page: number = 1, pageSize: number = 25): Promise<PaginatedResponse<AuditLogDto>> {
  return apiClient.get<PaginatedResponse<AuditLogDto>>(`/audit/users/${userId}`, { params: { page, pageSize } }).then((r) => r.data);
}
