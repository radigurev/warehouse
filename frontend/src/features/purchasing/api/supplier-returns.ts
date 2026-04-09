import apiClient from '@shared/api/client';
import type {
  SupplierReturnDto,
  SupplierReturnDetailDto,
  CreateSupplierReturnRequest,
  SearchSupplierReturnsRequest,
} from '@features/purchasing/types/purchasing';
import type { PaginatedResponse } from '@shared/types/api';

export function searchSupplierReturns(params: SearchSupplierReturnsRequest): Promise<PaginatedResponse<SupplierReturnDto>> {
  return apiClient.get<PaginatedResponse<SupplierReturnDto>>('/supplier-returns', { params }).then((r) => r.data);
}

export function getSupplierReturnById(id: number): Promise<SupplierReturnDetailDto> {
  return apiClient.get<SupplierReturnDetailDto>(`/supplier-returns/${id}`).then((r) => r.data);
}

export function createSupplierReturn(request: CreateSupplierReturnRequest): Promise<SupplierReturnDetailDto> {
  return apiClient.post<SupplierReturnDetailDto>('/supplier-returns', request).then((r) => r.data);
}

export function confirmSupplierReturn(id: number): Promise<void> {
  return apiClient.post(`/supplier-returns/${id}/confirm`, {}).then(() => undefined);
}

export function cancelSupplierReturn(id: number): Promise<void> {
  return apiClient.post(`/supplier-returns/${id}/cancel`, {}).then(() => undefined);
}
