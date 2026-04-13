import apiClient from '@shared/api/client';
import type { PaginatedResponse } from '@shared/types/api';

export interface CrudApi<TDto, TDetail, TCreate, TUpdate, TSearch> {
  search(params: TSearch): Promise<PaginatedResponse<TDto>>;
  getById(id: number): Promise<TDetail>;
  create(request: TCreate): Promise<TDetail>;
  update(id: number, request: TUpdate): Promise<TDetail>;
  remove(id: number): Promise<void>;
}

/**
 * Creates a standard CRUD API object for a given endpoint path.
 * The path is relative to the apiClient base URL (/api/v1).
 *
 * @example
 * const productApi = createCrudApi<ProductDto, ProductDetailDto, CreateProductRequest, UpdateProductRequest, SearchProductsRequest>('/products');
 * const { search, getById, create, update, remove } = productApi;
 */
export function createCrudApi<TDto, TDetail, TCreate, TUpdate, TSearch>(
  basePath: string,
): CrudApi<TDto, TDetail, TCreate, TUpdate, TSearch> {
  return {
    search: (params: TSearch): Promise<PaginatedResponse<TDto>> =>
      apiClient.get<PaginatedResponse<TDto>>(basePath, { params }).then((r) => r.data),

    getById: (id: number): Promise<TDetail> =>
      apiClient.get<TDetail>(`${basePath}/${id}`).then((r) => r.data),

    create: (request: TCreate): Promise<TDetail> =>
      apiClient.post<TDetail>(basePath, request).then((r) => r.data),

    update: (id: number, request: TUpdate): Promise<TDetail> =>
      apiClient.put<TDetail>(`${basePath}/${id}`, request).then((r) => r.data),

    remove: (id: number): Promise<void> =>
      apiClient.delete(`${basePath}/${id}`).then(() => undefined),
  };
}
