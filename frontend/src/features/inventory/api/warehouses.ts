import apiClient from '@shared/api/client';
import type {
  WarehouseDto,
  WarehouseDetailDto,
  CreateWarehouseRequest,
  UpdateWarehouseRequest,
  SearchWarehousesRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchWarehouses(params: SearchWarehousesRequest): Promise<PaginatedResponse<WarehouseDto>> {
  return apiClient.get<PaginatedResponse<WarehouseDto>>('/warehouses', { params }).then((r) => r.data);
}

export function getWarehouseById(id: number): Promise<WarehouseDetailDto> {
  return apiClient.get<WarehouseDetailDto>(`/warehouses/${id}`).then((r) => r.data);
}

export function createWarehouse(request: CreateWarehouseRequest): Promise<WarehouseDetailDto> {
  return apiClient.post<WarehouseDetailDto>('/warehouses', request).then((r) => r.data);
}

export function updateWarehouse(id: number, request: UpdateWarehouseRequest): Promise<WarehouseDetailDto> {
  return apiClient.put<WarehouseDetailDto>(`/warehouses/${id}`, request).then((r) => r.data);
}

export function deactivateWarehouse(id: number): Promise<void> {
  return apiClient.delete(`/warehouses/${id}`).then(() => undefined);
}

export function reactivateWarehouse(id: number): Promise<WarehouseDetailDto> {
  return apiClient.post<WarehouseDetailDto>(`/warehouses/${id}/reactivate`, {}).then((r) => r.data);
}
