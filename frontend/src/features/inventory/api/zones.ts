import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  ZoneDto,
  ZoneDetailDto,
  CreateZoneRequest,
  UpdateZoneRequest,
  SearchZonesRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<ZoneDto, ZoneDetailDto, CreateZoneRequest, UpdateZoneRequest, SearchZonesRequest>(
  '/zones',
);

export const searchZones = base.search;
export const getZoneById = base.getById;
export const createZone = base.create;
export const updateZone = base.update;
export const deleteZone = base.remove;
