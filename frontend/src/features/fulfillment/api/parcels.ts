import apiClient from '@shared/api/client';
import type {
  ParcelDto,
  CreateParcelRequest,
  UpdateParcelRequest,
  AddParcelItemRequest,
  ParcelItemDto,
} from '@features/fulfillment/types/fulfillment';

export function createParcel(soId: number, request: CreateParcelRequest): Promise<ParcelDto> {
  return apiClient.post<ParcelDto>(`/sales-orders/${soId}/parcels`, request).then((r) => r.data);
}

export function updateParcel(soId: number, parcelId: number, request: UpdateParcelRequest): Promise<ParcelDto> {
  return apiClient.put<ParcelDto>(`/sales-orders/${soId}/parcels/${parcelId}`, request).then((r) => r.data);
}

export function deleteParcel(soId: number, parcelId: number): Promise<void> {
  return apiClient.delete(`/sales-orders/${soId}/parcels/${parcelId}`).then(() => undefined);
}

export function addParcelItem(soId: number, parcelId: number, request: AddParcelItemRequest): Promise<ParcelItemDto> {
  return apiClient.post<ParcelItemDto>(`/sales-orders/${soId}/parcels/${parcelId}/items`, request).then((r) => r.data);
}

export function removeParcelItem(soId: number, parcelId: number, itemId: number): Promise<void> {
  return apiClient.delete(`/sales-orders/${soId}/parcels/${parcelId}/items/${itemId}`).then(() => undefined);
}
