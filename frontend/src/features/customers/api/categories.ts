import apiClient from '@shared/api/client';
import type {
  CustomerCategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@features/customers/types/customer';

export function getCategories(): Promise<CustomerCategoryDto[]> {
  return apiClient.get<CustomerCategoryDto[]>('/customer-categories').then((r) => r.data);
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
