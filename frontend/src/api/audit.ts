import apiClient from './client';
import type { AuditLogDto } from '@/types/audit';

export interface AuditQueryParams {
  page?: number;
  pageSize?: number;
  userId?: number;
  action?: string;
  dateFrom?: string;
  dateTo?: string;
}

export function getAuditLogs(params: AuditQueryParams = {}): Promise<AuditLogDto[]> {
  return apiClient.get<AuditLogDto[]>('/audit', { params }).then((r) => r.data);
}

export function getAuditLogsByUser(userId: number, page: number = 1, pageSize: number = 25): Promise<AuditLogDto[]> {
  return apiClient.get<AuditLogDto[]>(`/audit/users/${userId}`, { params: { page, pageSize } }).then((r) => r.data);
}
