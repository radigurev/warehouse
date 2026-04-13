import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  StorageLocationDto,
  CreateStorageLocationRequest,
  UpdateStorageLocationRequest,
  SearchStorageLocationsRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<StorageLocationDto, StorageLocationDto, CreateStorageLocationRequest, UpdateStorageLocationRequest, SearchStorageLocationsRequest>(
  '/storage-locations',
);

export const searchLocations = base.search;
export const getLocationById = base.getById;
export const createLocation = base.create;
export const updateLocation = base.update;
export const deleteLocation = base.remove;
