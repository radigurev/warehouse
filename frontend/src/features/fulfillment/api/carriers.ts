import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  CarrierDto,
  CarrierDetailDto,
  CarrierServiceLevelDto,
  CreateCarrierRequest,
  UpdateCarrierRequest,
  SearchCarriersRequest,
  CreateServiceLevelRequest,
  UpdateServiceLevelRequest,
} from '@features/fulfillment/types/fulfillment';

const base = createCrudApi<CarrierDto, CarrierDetailDto, CreateCarrierRequest, UpdateCarrierRequest, SearchCarriersRequest>(
  '/carriers',
);

export const searchCarriers = base.search;
export const getCarrierById = base.getById;
export const createCarrier = base.create;
export const updateCarrier = base.update;

export function deactivateCarrier(id: number): Promise<void> {
  return apiClient.post(`/carriers/${id}/deactivate`, {}).then(() => undefined);
}

export function createServiceLevel(carrierId: number, request: CreateServiceLevelRequest): Promise<CarrierServiceLevelDto> {
  return apiClient.post<CarrierServiceLevelDto>(`/carriers/${carrierId}/service-levels`, request).then((r) => r.data);
}

export function updateServiceLevel(carrierId: number, levelId: number, request: UpdateServiceLevelRequest): Promise<CarrierServiceLevelDto> {
  return apiClient.put<CarrierServiceLevelDto>(`/carriers/${carrierId}/service-levels/${levelId}`, request).then((r) => r.data);
}

export function deleteServiceLevel(carrierId: number, levelId: number): Promise<void> {
  return apiClient.delete(`/carriers/${carrierId}/service-levels/${levelId}`).then(() => undefined);
}
