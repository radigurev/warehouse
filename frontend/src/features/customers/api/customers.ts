import apiClient from '@shared/api/client';
import type {
  CustomerDto,
  CustomerDetailDto,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  SearchCustomersRequest,
} from '@features/customers/types/customer';
import type { PaginatedResponse } from '@shared/types/api';

export function searchCustomers(params: SearchCustomersRequest): Promise<PaginatedResponse<CustomerDto>> {
  return apiClient.get<PaginatedResponse<CustomerDto>>('/customers', { params }).then((r) => r.data);
}

export function getCustomerById(id: number): Promise<CustomerDetailDto> {
  return apiClient.get<CustomerDetailDto>(`/customers/${id}`).then((r) => r.data);
}

export function createCustomer(request: CreateCustomerRequest): Promise<CustomerDetailDto> {
  return apiClient.post<CustomerDetailDto>('/customers', request).then((r) => r.data);
}

export function updateCustomer(id: number, request: UpdateCustomerRequest): Promise<CustomerDetailDto> {
  return apiClient.put<CustomerDetailDto>(`/customers/${id}`, request).then((r) => r.data);
}

export function deactivateCustomer(id: number): Promise<void> {
  return apiClient.delete(`/customers/${id}`).then(() => undefined);
}

export function reactivateCustomer(id: number): Promise<CustomerDetailDto> {
  return apiClient.post<CustomerDetailDto>(`/customers/${id}/reactivate`, {}).then((r) => r.data);
}
