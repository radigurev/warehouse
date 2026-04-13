import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  ProductDto,
  ProductDetailDto,
  CreateProductRequest,
  UpdateProductRequest,
  SearchProductsRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<ProductDto, ProductDetailDto, CreateProductRequest, UpdateProductRequest, SearchProductsRequest>(
  '/products',
);

export const searchProducts = base.search;
export const getProductById = base.getById;
export const createProduct = base.create;
export const updateProduct = base.update;
export const deactivateProduct = base.remove;

export function reactivateProduct(id: number): Promise<ProductDetailDto> {
  return apiClient.post<ProductDetailDto>(`/products/${id}/reactivate`, {}).then((r) => r.data);
}
