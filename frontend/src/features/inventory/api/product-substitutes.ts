import apiClient from '@shared/api/client';
import type {
  ProductSubstituteDto,
  CreateProductSubstituteRequest,
} from '@features/inventory/types/inventory';

export function getSubstitutes(productId: number): Promise<ProductSubstituteDto[]> {
  return apiClient.get<ProductSubstituteDto[]>(`/products/${productId}/substitutes`).then((r) => r.data);
}

export function createSubstitute(request: CreateProductSubstituteRequest): Promise<ProductSubstituteDto> {
  return apiClient.post<ProductSubstituteDto>('/product-substitutes', request).then((r) => r.data);
}

export function deleteSubstitute(id: number): Promise<void> {
  return apiClient.delete(`/product-substitutes/${id}`).then(() => undefined);
}
