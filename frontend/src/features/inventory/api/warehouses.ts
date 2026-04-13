import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  WarehouseDto,
  WarehouseDetailDto,
  CreateWarehouseRequest,
  UpdateWarehouseRequest,
  SearchWarehousesRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<WarehouseDto, WarehouseDetailDto, CreateWarehouseRequest, UpdateWarehouseRequest, SearchWarehousesRequest>(
  '/warehouses',
);

export const searchWarehouses = base.search;
export const getWarehouseById = base.getById;
export const createWarehouse = base.create;
export const updateWarehouse = base.update;
export const deactivateWarehouse = base.remove;

export function reactivateWarehouse(id: number): Promise<WarehouseDetailDto> {
  return apiClient.post<WarehouseDetailDto>(`/warehouses/${id}/reactivate`, {}).then((r) => r.data);
}
