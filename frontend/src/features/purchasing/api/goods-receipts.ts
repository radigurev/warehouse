import apiClient from '@shared/api/client';
import type {
  GoodsReceiptDto,
  GoodsReceiptDetailDto,
  CreateGoodsReceiptRequest,
  SearchGoodsReceiptsRequest,
  InspectLineRequest,
  ResolveQuarantineRequest,
} from '@features/purchasing/types/purchasing';
import type { PaginatedResponse } from '@shared/types/api';

export function searchGoodsReceipts(params: SearchGoodsReceiptsRequest): Promise<PaginatedResponse<GoodsReceiptDto>> {
  return apiClient.get<PaginatedResponse<GoodsReceiptDto>>('/goods-receipts', { params }).then((r) => r.data);
}

export function getGoodsReceiptById(id: number): Promise<GoodsReceiptDetailDto> {
  return apiClient.get<GoodsReceiptDetailDto>(`/goods-receipts/${id}`).then((r) => r.data);
}

export function createGoodsReceipt(request: CreateGoodsReceiptRequest): Promise<GoodsReceiptDetailDto> {
  return apiClient.post<GoodsReceiptDetailDto>('/goods-receipts', request).then((r) => r.data);
}

export function completeGoodsReceipt(id: number): Promise<void> {
  return apiClient.post(`/goods-receipts/${id}/complete`, {}).then(() => undefined);
}

export function inspectReceiptLine(receiptId: number, lineId: number, request: InspectLineRequest): Promise<void> {
  return apiClient.post(`/goods-receipts/${receiptId}/lines/${lineId}/inspect`, request).then(() => undefined);
}

export function resolveQuarantinedLine(receiptId: number, lineId: number, request: ResolveQuarantineRequest): Promise<void> {
  return apiClient.post(`/goods-receipts/${receiptId}/lines/${lineId}/resolve`, request).then(() => undefined);
}
