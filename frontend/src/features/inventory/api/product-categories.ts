import apiClient from '@shared/api/client';
import type {
  ProductCategoryDto,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '@features/inventory/types/inventory';

export function searchCategories(): Promise<ProductCategoryDto[]> {
  return apiClient.get<ProductCategoryDto[]>('/product-categories').then((r) => r.data);
}

export function getCategoryById(id: number): Promise<ProductCategoryDto> {
  return apiClient.get<ProductCategoryDto>(`/product-categories/${id}`).then((r) => r.data);
}

export function createCategory(request: CreateProductCategoryRequest): Promise<ProductCategoryDto> {
  return apiClient.post<ProductCategoryDto>('/product-categories', request).then((r) => r.data);
}

export function updateCategory(id: number, request: UpdateProductCategoryRequest): Promise<ProductCategoryDto> {
  return apiClient.put<ProductCategoryDto>(`/product-categories/${id}`, request).then((r) => r.data);
}

export function deleteCategory(id: number): Promise<void> {
  return apiClient.delete(`/product-categories/${id}`).then(() => undefined);
}
