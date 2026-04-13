import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  InventoryAdjustmentDto,
  InventoryAdjustmentDetailDto,
  CreateAdjustmentRequest,
  ApproveAdjustmentRequest,
  RejectAdjustmentRequest,
  SearchAdjustmentsRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<InventoryAdjustmentDto, InventoryAdjustmentDetailDto, CreateAdjustmentRequest, never, SearchAdjustmentsRequest>(
  '/adjustments',
);

export const searchAdjustments = base.search;
export const getAdjustmentById = base.getById;
export const createAdjustment = base.create;

export function approveAdjustment(id: number, request: ApproveAdjustmentRequest): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>(`/adjustments/${id}/approve`, request).then((r) => r.data);
}

export function rejectAdjustment(id: number, request: RejectAdjustmentRequest): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>(`/adjustments/${id}/reject`, request).then((r) => r.data);
}

export function applyAdjustment(id: number): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>(`/adjustments/${id}/apply`, {}).then((r) => r.data);
}
