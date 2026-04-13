import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  WarehouseTransferDto,
  WarehouseTransferDetailDto,
  CreateTransferRequest,
  CompleteTransferRequest,
  SearchTransfersRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<WarehouseTransferDto, WarehouseTransferDetailDto, CreateTransferRequest, never, SearchTransfersRequest>(
  '/transfers',
);

export const searchTransfers = base.search;
export const getTransferById = base.getById;
export const createTransfer = base.create;

export function completeTransfer(id: number, request: CompleteTransferRequest): Promise<WarehouseTransferDetailDto> {
  return apiClient.post<WarehouseTransferDetailDto>(`/transfers/${id}/complete`, request).then((r) => r.data);
}

export function cancelTransfer(id: number): Promise<WarehouseTransferDetailDto> {
  return apiClient.post<WarehouseTransferDetailDto>(`/transfers/${id}/cancel`, {}).then((r) => r.data);
}
