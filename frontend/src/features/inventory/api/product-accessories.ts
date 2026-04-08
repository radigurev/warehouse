import apiClient from '@shared/api/client';
import type {
  ProductAccessoryDto,
  CreateProductAccessoryRequest,
} from '@features/inventory/types/inventory';

export function getAccessories(productId: number): Promise<ProductAccessoryDto[]> {
  return apiClient.get<ProductAccessoryDto[]>(`/product-accessories/by-product/${productId}`).then((r) => r.data);
}

export function createAccessory(request: CreateProductAccessoryRequest): Promise<ProductAccessoryDto> {
  return apiClient.post<ProductAccessoryDto>('/product-accessories', request).then((r) => r.data);
}

export function deleteAccessory(id: number): Promise<void> {
  return apiClient.delete(`/product-accessories/${id}`).then(() => undefined);
}
