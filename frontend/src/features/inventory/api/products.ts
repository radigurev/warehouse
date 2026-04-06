import apiClient from '@shared/api/client';
import type {
  ProductDto,
  ProductDetailDto,
  CreateProductRequest,
  UpdateProductRequest,
  SearchProductsRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchProducts(params: SearchProductsRequest): Promise<PaginatedResponse<ProductDto>> {
  return apiClient.get<PaginatedResponse<ProductDto>>('/products', { params }).then((r) => r.data);
}

export function getProductById(id: number): Promise<ProductDetailDto> {
  return apiClient.get<ProductDetailDto>(`/products/${id}`).then((r) => r.data);
}

export function createProduct(request: CreateProductRequest): Promise<ProductDetailDto> {
  return apiClient.post<ProductDetailDto>('/products', request).then((r) => r.data);
}

export function updateProduct(id: number, request: UpdateProductRequest): Promise<ProductDetailDto> {
  return apiClient.put<ProductDetailDto>(`/products/${id}`, request).then((r) => r.data);
}

export function deactivateProduct(id: number): Promise<void> {
  return apiClient.delete(`/products/${id}`).then(() => undefined);
}

export function reactivateProduct(id: number): Promise<ProductDetailDto> {
  return apiClient.post<ProductDetailDto>(`/products/${id}/reactivate`, {}).then((r) => r.data);
}
