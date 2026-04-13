import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  StockLevelDto,
  StockSummaryDto,
  SearchStockLevelsRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<StockLevelDto, StockLevelDto, never, never, SearchStockLevelsRequest>(
  '/stock-levels',
);

export const searchStockLevels = base.search;
export const getStockLevelById = base.getById;

export function getStockSummary(productId: number): Promise<StockSummaryDto> {
  return apiClient.get<StockSummaryDto>(`/stock-levels/summary/${productId}`).then((r) => r.data);
}
