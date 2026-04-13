import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  GoodsReceiptDto,
  GoodsReceiptDetailDto,
  CreateGoodsReceiptRequest,
  SearchGoodsReceiptsRequest,
  InspectLineRequest,
  ResolveQuarantineRequest,
} from '@features/purchasing/types/purchasing';

const base = createCrudApi<GoodsReceiptDto, GoodsReceiptDetailDto, CreateGoodsReceiptRequest, never, SearchGoodsReceiptsRequest>(
  '/goods-receipts',
);

export const searchGoodsReceipts = base.search;
export const getGoodsReceiptById = base.getById;
export const createGoodsReceipt = base.create;

export function completeGoodsReceipt(id: number): Promise<void> {
  return apiClient.post(`/goods-receipts/${id}/complete`, {}).then(() => undefined);
}

export function inspectReceiptLine(receiptId: number, lineId: number, request: InspectLineRequest): Promise<void> {
  return apiClient.post(`/goods-receipts/${receiptId}/lines/${lineId}/inspect`, request).then(() => undefined);
}

export function resolveQuarantinedLine(receiptId: number, lineId: number, request: ResolveQuarantineRequest): Promise<void> {
  return apiClient.post(`/goods-receipts/${receiptId}/lines/${lineId}/resolve`, request).then(() => undefined);
}
