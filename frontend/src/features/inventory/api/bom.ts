import apiClient from '@shared/api/client';
import type {
  BomDto,
  CreateBomRequest,
  UpdateBomRequest,
  AddBomLineRequest,
  BomLineDto,
} from '@features/inventory/types/inventory';

export function getBomByProductId(productId: number): Promise<BomDto | null> {
  return apiClient.get<BomDto[]>(`/bom/by-product/${productId}`).then((r) => r.data[0] ?? null);
}

export function createBom(request: CreateBomRequest): Promise<BomDto> {
  return apiClient.post<BomDto>('/bom', request).then((r) => r.data);
}

export function updateBom(bomId: number, request: UpdateBomRequest): Promise<BomDto> {
  return apiClient.put<BomDto>(`/bom/${bomId}`, request).then((r) => r.data);
}

export function deleteBom(bomId: number): Promise<void> {
  return apiClient.delete(`/bom/${bomId}`).then(() => undefined);
}

export function addBomLine(bomId: number, request: AddBomLineRequest): Promise<BomLineDto> {
  return apiClient.post<BomLineDto>(`/bom/${bomId}/lines`, request).then((r) => r.data);
}

export function removeBomLine(bomId: number, lineId: number): Promise<void> {
  return apiClient.delete(`/bom/${bomId}/lines/${lineId}`).then(() => undefined);
}
