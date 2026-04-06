import apiClient from '@shared/api/client';
import type {
  InventoryAdjustmentDto,
  InventoryAdjustmentDetailDto,
  CreateAdjustmentRequest,
  ApproveAdjustmentRequest,
  RejectAdjustmentRequest,
  SearchAdjustmentsRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchAdjustments(params: SearchAdjustmentsRequest): Promise<PaginatedResponse<InventoryAdjustmentDto>> {
  return apiClient.get<PaginatedResponse<InventoryAdjustmentDto>>('/adjustments', { params }).then((r) => r.data);
}

export function getAdjustmentById(id: number): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.get<InventoryAdjustmentDetailDto>(`/adjustments/${id}`).then((r) => r.data);
}

export function createAdjustment(request: CreateAdjustmentRequest): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>('/adjustments', request).then((r) => r.data);
}

export function approveAdjustment(id: number, request: ApproveAdjustmentRequest): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>(`/adjustments/${id}/approve`, request).then((r) => r.data);
}

export function rejectAdjustment(id: number, request: RejectAdjustmentRequest): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>(`/adjustments/${id}/reject`, request).then((r) => r.data);
}

export function applyAdjustment(id: number): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>(`/adjustments/${id}/apply`, {}).then((r) => r.data);
}
