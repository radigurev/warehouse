import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type { PaginatedResponse } from '@shared/types/api';
import type {
  ProductCategoryDto,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<ProductCategoryDto, ProductCategoryDto, CreateProductCategoryRequest, UpdateProductCategoryRequest, { page: number; pageSize: number }>(
  '/product-categories',
);

export function searchCategories(page = 1, pageSize = 25): Promise<PaginatedResponse<ProductCategoryDto>> {
  return base.search({ page, pageSize });
}

export function getAllCategories(): Promise<ProductCategoryDto[]> {
  return apiClient
    .get<PaginatedResponse<ProductCategoryDto>>('/product-categories', { params: { page: 1, pageSize: 100 } })
    .then((r) => r.data.items);
}

export const getCategoryById = base.getById;
export const createCategory = base.create;
export const updateCategory = base.update;
export const deleteCategory = base.remove;
