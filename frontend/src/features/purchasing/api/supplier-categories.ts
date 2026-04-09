import apiClient from '@shared/api/client';
import type {
  SupplierCategoryDto,
  CreateSupplierCategoryRequest,
  UpdateSupplierCategoryRequest,
} from '@features/purchasing/types/purchasing';

export function getAllSupplierCategories(): Promise<SupplierCategoryDto[]> {
  return apiClient.get<SupplierCategoryDto[]>('/supplier-categories').then((r) => r.data);
}

export function createSupplierCategory(request: CreateSupplierCategoryRequest): Promise<SupplierCategoryDto> {
  return apiClient.post<SupplierCategoryDto>('/supplier-categories', request).then((r) => r.data);
}

export function updateSupplierCategory(id: number, request: UpdateSupplierCategoryRequest): Promise<SupplierCategoryDto> {
  return apiClient.put<SupplierCategoryDto>(`/supplier-categories/${id}`, request).then((r) => r.data);
}

export function deleteSupplierCategory(id: number): Promise<void> {
  return apiClient.delete(`/supplier-categories/${id}`).then(() => undefined);
}
