import apiClient from '@shared/api/client';
import type {
  CustomerReturnDto,
  CustomerReturnDetailDto,
  CreateCustomerReturnRequest,
  SearchCustomerReturnsRequest,
} from '@features/fulfillment/types/fulfillment';
import type { PaginatedResponse } from '@shared/types/api';

export function searchCustomerReturns(params: SearchCustomerReturnsRequest): Promise<PaginatedResponse<CustomerReturnDto>> {
  return apiClient.get<PaginatedResponse<CustomerReturnDto>>('/customer-returns', { params }).then((r) => r.data);
}

export function getCustomerReturnById(id: number): Promise<CustomerReturnDetailDto> {
  return apiClient.get<CustomerReturnDetailDto>(`/customer-returns/${id}`).then((r) => r.data);
}

export function createCustomerReturn(request: CreateCustomerReturnRequest): Promise<CustomerReturnDetailDto> {
  return apiClient.post<CustomerReturnDetailDto>('/customer-returns', request).then((r) => r.data);
}

export function confirmCustomerReturn(id: number): Promise<void> {
  return apiClient.post(`/customer-returns/${id}/confirm`, {}).then(() => undefined);
}

export function receiveCustomerReturn(id: number): Promise<void> {
  return apiClient.post(`/customer-returns/${id}/receive`, {}).then(() => undefined);
}

export function closeCustomerReturn(id: number): Promise<void> {
  return apiClient.post(`/customer-returns/${id}/close`, {}).then(() => undefined);
}

export function cancelCustomerReturn(id: number): Promise<void> {
  return apiClient.post(`/customer-returns/${id}/cancel`, {}).then(() => undefined);
}
