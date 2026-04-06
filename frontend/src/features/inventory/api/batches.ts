import apiClient from '@shared/api/client';
import type {
  BatchDto,
  CreateBatchRequest,
  UpdateBatchRequest,
  SearchBatchesRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchBatches(params: SearchBatchesRequest): Promise<PaginatedResponse<BatchDto>> {
  return apiClient.get<PaginatedResponse<BatchDto>>('/batches', { params }).then((r) => r.data);
}

export function getBatchById(id: number): Promise<BatchDto> {
  return apiClient.get<BatchDto>(`/batches/${id}`).then((r) => r.data);
}

export function createBatch(request: CreateBatchRequest): Promise<BatchDto> {
  return apiClient.post<BatchDto>('/batches', request).then((r) => r.data);
}

export function updateBatch(id: number, request: UpdateBatchRequest): Promise<BatchDto> {
  return apiClient.put<BatchDto>(`/batches/${id}`, request).then((r) => r.data);
}

export function deactivateBatch(id: number): Promise<void> {
  return apiClient.delete(`/batches/${id}`).then(() => undefined);
}
