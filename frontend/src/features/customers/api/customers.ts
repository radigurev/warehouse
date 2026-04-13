import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  CustomerDto,
  CustomerDetailDto,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  SearchCustomersRequest,
} from '@features/customers/types/customer';

const base = createCrudApi<CustomerDto, CustomerDetailDto, CreateCustomerRequest, UpdateCustomerRequest, SearchCustomersRequest>(
  '/customers',
);

export const searchCustomers = base.search;
export const getCustomerById = base.getById;
export const createCustomer = base.create;
export const updateCustomer = base.update;
export const deactivateCustomer = base.remove;

export function reactivateCustomer(id: number): Promise<CustomerDetailDto> {
  return apiClient.post<CustomerDetailDto>(`/customers/${id}/reactivate`, {}).then((r) => r.data);
}
