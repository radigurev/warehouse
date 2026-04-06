import apiClient from '@shared/api/client';
import type {
  StockLevelDto,
  StockSummaryDto,
  SearchStockLevelsRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchStockLevels(params: SearchStockLevelsRequest): Promise<PaginatedResponse<StockLevelDto>> {
  return apiClient.get<PaginatedResponse<StockLevelDto>>('/stock-levels', { params }).then((r) => r.data);
}

export function getStockLevelById(id: number): Promise<StockLevelDto> {
  return apiClient.get<StockLevelDto>(`/stock-levels/${id}`).then((r) => r.data);
}

export function getStockSummary(productId: number): Promise<StockSummaryDto> {
  return apiClient.get<StockSummaryDto>(`/stock-levels/summary/${productId}`).then((r) => r.data);
}
