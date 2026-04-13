import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type { PaginatedResponse } from '@shared/types/api';
import type {
  UnitOfMeasureDto,
  CreateUnitOfMeasureRequest,
  UpdateUnitOfMeasureRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<UnitOfMeasureDto, UnitOfMeasureDto, CreateUnitOfMeasureRequest, UpdateUnitOfMeasureRequest, { page: number; pageSize: number }>(
  '/units-of-measure',
);

export function searchUnits(page = 1, pageSize = 25): Promise<PaginatedResponse<UnitOfMeasureDto>> {
  return base.search({ page, pageSize });
}

export function getAllUnits(): Promise<UnitOfMeasureDto[]> {
  return apiClient
    .get<PaginatedResponse<UnitOfMeasureDto>>('/units-of-measure', { params: { page: 1, pageSize: 100 } })
    .then((r) => r.data.items);
}

export const getUnitById = base.getById;
export const createUnit = base.create;
export const updateUnit = base.update;
export const deleteUnit = base.remove;
