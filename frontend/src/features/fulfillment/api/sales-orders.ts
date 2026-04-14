import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  SalesOrderDto,
  SalesOrderDetailDto,
  SalesOrderLineDto,
  CreateSalesOrderRequest,
  UpdateSalesOrderRequest,
  CreateSalesOrderLineRequest,
  UpdateSalesOrderLineRequest,
  SearchSalesOrdersRequest,
} from '@features/fulfillment/types/fulfillment';

const base = createCrudApi<SalesOrderDto, SalesOrderDetailDto, CreateSalesOrderRequest, UpdateSalesOrderRequest, SearchSalesOrdersRequest>(
  '/sales-orders',
);

export const searchSalesOrders = base.search;
export const getSalesOrderById = base.getById;
export const createSalesOrder = base.create;
export const updateSalesOrder = base.update;

export function confirmSalesOrder(id: number): Promise<void> {
  return apiClient.post(`/sales-orders/${id}/confirm`, {}).then(() => undefined);
}

export function cancelSalesOrder(id: number): Promise<void> {
  return apiClient.post(`/sales-orders/${id}/cancel`, {}).then(() => undefined);
}

export function completeSalesOrder(id: number): Promise<void> {
  return apiClient.post(`/sales-orders/${id}/complete`, {}).then(() => undefined);
}

export function addSalesOrderLine(soId: number, request: CreateSalesOrderLineRequest): Promise<SalesOrderLineDto> {
  return apiClient.post<SalesOrderLineDto>(`/sales-orders/${soId}/lines`, request).then((r) => r.data);
}

export function updateSalesOrderLine(soId: number, lineId: number, request: UpdateSalesOrderLineRequest): Promise<SalesOrderLineDto> {
  return apiClient.put<SalesOrderLineDto>(`/sales-orders/${soId}/lines/${lineId}`, request).then((r) => r.data);
}

export function deleteSalesOrderLine(soId: number, lineId: number): Promise<void> {
  return apiClient.delete(`/sales-orders/${soId}/lines/${lineId}`).then(() => undefined);
}
