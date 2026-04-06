import apiClient from '@shared/api/client';
import type {
  StorageLocationDto,
  CreateStorageLocationRequest,
  UpdateStorageLocationRequest,
  SearchStorageLocationsRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchLocations(params: SearchStorageLocationsRequest): Promise<PaginatedResponse<StorageLocationDto>> {
  return apiClient.get<PaginatedResponse<StorageLocationDto>>('/storage-locations', { params }).then((r) => r.data);
}

export function getLocationById(id: number): Promise<StorageLocationDto> {
  return apiClient.get<StorageLocationDto>(`/storage-locations/${id}`).then((r) => r.data);
}

export function createLocation(request: CreateStorageLocationRequest): Promise<StorageLocationDto> {
  return apiClient.post<StorageLocationDto>('/storage-locations', request).then((r) => r.data);
}

export function updateLocation(id: number, request: UpdateStorageLocationRequest): Promise<StorageLocationDto> {
  return apiClient.put<StorageLocationDto>(`/storage-locations/${id}`, request).then((r) => r.data);
}

export function deleteLocation(id: number): Promise<void> {
  return apiClient.delete(`/storage-locations/${id}`).then(() => undefined);
}
