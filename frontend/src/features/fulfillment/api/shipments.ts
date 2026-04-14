import apiClient from '@shared/api/client';
import type {
  ShipmentDto,
  ShipmentDetailDto,
  CreateShipmentRequest,
  UpdateShipmentStatusRequest,
  SearchShipmentsRequest,
} from '@features/fulfillment/types/fulfillment';
import type { PaginatedResponse } from '@shared/types/api';

export function searchShipments(params: SearchShipmentsRequest): Promise<PaginatedResponse<ShipmentDto>> {
  return apiClient.get<PaginatedResponse<ShipmentDto>>('/shipments', { params }).then((r) => r.data);
}

export function getShipmentById(id: number): Promise<ShipmentDetailDto> {
  return apiClient.get<ShipmentDetailDto>(`/shipments/${id}`).then((r) => r.data);
}

export function createShipment(request: CreateShipmentRequest): Promise<ShipmentDetailDto> {
  return apiClient.post<ShipmentDetailDto>('/shipments', request).then((r) => r.data);
}

export function updateShipmentStatus(id: number, request: UpdateShipmentStatusRequest): Promise<void> {
  return apiClient.post(`/shipments/${id}/status`, request).then(() => undefined);
}
