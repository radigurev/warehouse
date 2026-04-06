import apiClient from '@shared/api/client';
import type {
  ZoneDto,
  ZoneDetailDto,
  CreateZoneRequest,
  UpdateZoneRequest,
  SearchZonesRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchZones(params: SearchZonesRequest): Promise<PaginatedResponse<ZoneDto>> {
  return apiClient.get<PaginatedResponse<ZoneDto>>('/zones', { params }).then((r) => r.data);
}

export function getZoneById(id: number): Promise<ZoneDetailDto> {
  return apiClient.get<ZoneDetailDto>(`/zones/${id}`).then((r) => r.data);
}

export function createZone(request: CreateZoneRequest): Promise<ZoneDetailDto> {
  return apiClient.post<ZoneDetailDto>('/zones', request).then((r) => r.data);
}

export function updateZone(id: number, request: UpdateZoneRequest): Promise<ZoneDetailDto> {
  return apiClient.put<ZoneDetailDto>(`/zones/${id}`, request).then((r) => r.data);
}

export function deleteZone(id: number): Promise<void> {
  return apiClient.delete(`/zones/${id}`).then(() => undefined);
}
