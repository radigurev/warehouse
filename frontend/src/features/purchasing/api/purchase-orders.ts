import apiClient from '@shared/api/client';
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
import type { PaginatedResponse } from '@shared/types/api';

export function searchPurchaseOrders(params: SearchPurchaseOrdersRequest): Promise<PaginatedResponse<PurchaseOrderDto>> {
  return apiClient.get<PaginatedResponse<PurchaseOrderDto>>('/purchase-orders', { params }).then((r) => r.data);
}

export function getPurchaseOrderById(id: number): Promise<PurchaseOrderDetailDto> {
  return apiClient.get<PurchaseOrderDetailDto>(`/purchase-orders/${id}`).then((r) => r.data);
}

export function createPurchaseOrder(request: CreatePurchaseOrderRequest): Promise<PurchaseOrderDetailDto> {
  return apiClient.post<PurchaseOrderDetailDto>('/purchase-orders', request).then((r) => r.data);
}

export function updatePurchaseOrder(id: number, request: UpdatePurchaseOrderRequest): Promise<PurchaseOrderDetailDto> {
  return apiClient.put<PurchaseOrderDetailDto>(`/purchase-orders/${id}`, request).then((r) => r.data);
}

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
