import apiClient from '@shared/api/client';
import type {
  WarehouseTransferDto,
  WarehouseTransferDetailDto,
  CreateTransferRequest,
  CompleteTransferRequest,
  SearchTransfersRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchTransfers(params: SearchTransfersRequest): Promise<PaginatedResponse<WarehouseTransferDto>> {
  return apiClient.get<PaginatedResponse<WarehouseTransferDto>>('/transfers', { params }).then((r) => r.data);
}

export function getTransferById(id: number): Promise<WarehouseTransferDetailDto> {
  return apiClient.get<WarehouseTransferDetailDto>(`/transfers/${id}`).then((r) => r.data);
}

export function createTransfer(request: CreateTransferRequest): Promise<WarehouseTransferDetailDto> {
  return apiClient.post<WarehouseTransferDetailDto>('/transfers', request).then((r) => r.data);
}

export function completeTransfer(id: number, request: CompleteTransferRequest): Promise<WarehouseTransferDetailDto> {
  return apiClient.post<WarehouseTransferDetailDto>(`/transfers/${id}/complete`, request).then((r) => r.data);
}

export function cancelTransfer(id: number): Promise<WarehouseTransferDetailDto> {
  return apiClient.post<WarehouseTransferDetailDto>(`/transfers/${id}/cancel`, {}).then((r) => r.data);
}
