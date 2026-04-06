import apiClient from '@shared/api/client';
import type {
  StockMovementDto,
  SearchStockMovementsRequest,
  RecordStockMovementRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchMovements(params: SearchStockMovementsRequest): Promise<PaginatedResponse<StockMovementDto>> {
  return apiClient.get<PaginatedResponse<StockMovementDto>>('/stock-movements', { params }).then((r) => r.data);
}

export function recordMovement(request: RecordStockMovementRequest): Promise<StockMovementDto> {
  return apiClient.post<StockMovementDto>('/stock-movements', request).then((r) => r.data);
}
