import apiClient from '@shared/api/client';
import type { PaginatedResponse } from '@shared/types/api';
import type {
  CustomerCategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@features/customers/types/customer';

export function getCategories(page = 1, pageSize = 25): Promise<PaginatedResponse<CustomerCategoryDto>> {
  return apiClient
    .get<PaginatedResponse<CustomerCategoryDto>>('/customer-categories', { params: { page, pageSize } })
    .then((r) => r.data);
}

export function getAllCategories(): Promise<CustomerCategoryDto[]> {
  return apiClient
    .get<PaginatedResponse<CustomerCategoryDto>>('/customer-categories', { params: { page: 1, pageSize: 100 } })
    .then((r) => r.data.items);
}

export function getCategoryById(id: number): Promise<CustomerCategoryDto> {
  return apiClient.get<CustomerCategoryDto>(`/customer-categories/${id}`).then((r) => r.data);
}

export function createCategory(request: CreateCategoryRequest): Promise<CustomerCategoryDto> {
  return apiClient.post<CustomerCategoryDto>('/customer-categories', request).then((r) => r.data);
}

export function updateCategory(id: number, request: UpdateCategoryRequest): Promise<CustomerCategoryDto> {
  return apiClient.put<CustomerCategoryDto>(`/customer-categories/${id}`, request).then((r) => r.data);
}

export function deleteCategory(id: number): Promise<void> {
  return apiClient.delete(`/customer-categories/${id}`).then(() => undefined);
}
