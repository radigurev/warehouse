import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type { PaginatedResponse } from '@shared/types/api';
import type {
  CustomerCategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@features/customers/types/customer';

const base = createCrudApi<CustomerCategoryDto, CustomerCategoryDto, CreateCategoryRequest, UpdateCategoryRequest, { page: number; pageSize: number }>(
  '/customer-categories',
);

export function getCategories(page = 1, pageSize = 25): Promise<PaginatedResponse<CustomerCategoryDto>> {
  return base.search({ page, pageSize });
}

export function getAllCategories(): Promise<CustomerCategoryDto[]> {
  return apiClient
    .get<PaginatedResponse<CustomerCategoryDto>>('/customer-categories', { params: { page: 1, pageSize: 100 } })
    .then((r) => r.data.items);
}

export const getCategoryById = base.getById;
export const createCategory = base.create;
export const updateCategory = base.update;
export const deleteCategory = base.remove;
