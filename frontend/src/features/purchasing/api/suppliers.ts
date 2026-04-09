import apiClient from '@shared/api/client';
import type {
  SupplierDto,
  SupplierDetailDto,
  CreateSupplierRequest,
  UpdateSupplierRequest,
  SearchSuppliersRequest,
} from '@features/purchasing/types/purchasing';
import type { PaginatedResponse } from '@shared/types/api';

export function searchSuppliers(params: SearchSuppliersRequest): Promise<PaginatedResponse<SupplierDto>> {
  return apiClient.get<PaginatedResponse<SupplierDto>>('/suppliers', { params }).then((r) => r.data);
}

export function getSupplierById(id: number): Promise<SupplierDetailDto> {
  return apiClient.get<SupplierDetailDto>(`/suppliers/${id}`).then((r) => r.data);
}

export function createSupplier(request: CreateSupplierRequest): Promise<SupplierDetailDto> {
  return apiClient.post<SupplierDetailDto>('/suppliers', request).then((r) => r.data);
}

export function updateSupplier(id: number, request: UpdateSupplierRequest): Promise<SupplierDetailDto> {
  return apiClient.put<SupplierDetailDto>(`/suppliers/${id}`, request).then((r) => r.data);
}

export function deactivateSupplier(id: number): Promise<void> {
  return apiClient.delete(`/suppliers/${id}`).then(() => undefined);
}

export function reactivateSupplier(id: number): Promise<SupplierDetailDto> {
  return apiClient.post<SupplierDetailDto>(`/suppliers/${id}/reactivate`, {}).then((r) => r.data);
}
