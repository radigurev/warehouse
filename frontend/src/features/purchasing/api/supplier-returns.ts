import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  SupplierReturnDto,
  SupplierReturnDetailDto,
  CreateSupplierReturnRequest,
  SearchSupplierReturnsRequest,
} from '@features/purchasing/types/purchasing';

const base = createCrudApi<SupplierReturnDto, SupplierReturnDetailDto, CreateSupplierReturnRequest, never, SearchSupplierReturnsRequest>(
  '/supplier-returns',
);

export const searchSupplierReturns = base.search;
export const getSupplierReturnById = base.getById;
export const createSupplierReturn = base.create;

export function confirmSupplierReturn(id: number): Promise<void> {
  return apiClient.post(`/supplier-returns/${id}/confirm`, {}).then(() => undefined);
}

export function cancelSupplierReturn(id: number): Promise<void> {
  return apiClient.post(`/supplier-returns/${id}/cancel`, {}).then(() => undefined);
}
