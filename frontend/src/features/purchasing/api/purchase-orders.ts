import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  PurchaseOrderDto,
  PurchaseOrderDetailDto,
  PurchaseOrderLineDto,
  CreatePurchaseOrderRequest,
  UpdatePurchaseOrderRequest,
  CreatePurchaseOrderLineRequest,
  UpdatePurchaseOrderLineRequest,
  SearchPurchaseOrdersRequest,
} from '@features/purchasing/types/purchasing';

const base = createCrudApi<PurchaseOrderDto, PurchaseOrderDetailDto, CreatePurchaseOrderRequest, UpdatePurchaseOrderRequest, SearchPurchaseOrdersRequest>(
  '/purchase-orders',
);

export const searchPurchaseOrders = base.search;
export const getPurchaseOrderById = base.getById;
export const createPurchaseOrder = base.create;
export const updatePurchaseOrder = base.update;

export function confirmPurchaseOrder(id: number): Promise<void> {
  return apiClient.post(`/purchase-orders/${id}/confirm`, {}).then(() => undefined);
}

export function cancelPurchaseOrder(id: number): Promise<void> {
  return apiClient.post(`/purchase-orders/${id}/cancel`, {}).then(() => undefined);
}

export function closePurchaseOrder(id: number): Promise<void> {
  return apiClient.post(`/purchase-orders/${id}/close`, {}).then(() => undefined);
}

export function addPurchaseOrderLine(poId: number, request: CreatePurchaseOrderLineRequest): Promise<PurchaseOrderLineDto> {
  return apiClient.post<PurchaseOrderLineDto>(`/purchase-orders/${poId}/lines`, request).then((r) => r.data);
}

export function updatePurchaseOrderLine(poId: number, lineId: number, request: UpdatePurchaseOrderLineRequest): Promise<PurchaseOrderLineDto> {
  return apiClient.put<PurchaseOrderLineDto>(`/purchase-orders/${poId}/lines/${lineId}`, request).then((r) => r.data);
}

export function deletePurchaseOrderLine(poId: number, lineId: number): Promise<void> {
  return apiClient.delete(`/purchase-orders/${poId}/lines/${lineId}`).then(() => undefined);
}
