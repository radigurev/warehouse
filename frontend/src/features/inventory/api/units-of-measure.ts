import apiClient from '@shared/api/client';
import type { PaginatedResponse } from '@shared/types/api';
import type {
  UnitOfMeasureDto,
  CreateUnitOfMeasureRequest,
  UpdateUnitOfMeasureRequest,
} from '@features/inventory/types/inventory';

export function searchUnits(page = 1, pageSize = 25): Promise<PaginatedResponse<UnitOfMeasureDto>> {
  return apiClient
    .get<PaginatedResponse<UnitOfMeasureDto>>('/units-of-measure', { params: { page, pageSize } })
    .then((r) => r.data);
}

export function getAllUnits(): Promise<UnitOfMeasureDto[]> {
  return apiClient
    .get<PaginatedResponse<UnitOfMeasureDto>>('/units-of-measure', { params: { page: 1, pageSize: 100 } })
    .then((r) => r.data.items);
}

export function getUnitById(id: number): Promise<UnitOfMeasureDto> {
  return apiClient.get<UnitOfMeasureDto>(`/units-of-measure/${id}`).then((r) => r.data);
}

export function createUnit(request: CreateUnitOfMeasureRequest): Promise<UnitOfMeasureDto> {
  return apiClient.post<UnitOfMeasureDto>('/units-of-measure', request).then((r) => r.data);
}

export function updateUnit(id: number, request: UpdateUnitOfMeasureRequest): Promise<UnitOfMeasureDto> {
  return apiClient.put<UnitOfMeasureDto>(`/units-of-measure/${id}`, request).then((r) => r.data);
}

export function deleteUnit(id: number): Promise<void> {
  return apiClient.delete(`/units-of-measure/${id}`).then(() => undefined);
}
