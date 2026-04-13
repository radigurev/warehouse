import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  SupplierDto,
  SupplierDetailDto,
  CreateSupplierRequest,
  UpdateSupplierRequest,
  SearchSuppliersRequest,
} from '@features/purchasing/types/purchasing';

const base = createCrudApi<SupplierDto, SupplierDetailDto, CreateSupplierRequest, UpdateSupplierRequest, SearchSuppliersRequest>(
  '/suppliers',
);

export const searchSuppliers = base.search;
export const getSupplierById = base.getById;
export const createSupplier = base.create;
export const updateSupplier = base.update;
export const deactivateSupplier = base.remove;

export function reactivateSupplier(id: number): Promise<SupplierDetailDto> {
  return apiClient.post<SupplierDetailDto>(`/suppliers/${id}/reactivate`, {}).then((r) => r.data);
}
