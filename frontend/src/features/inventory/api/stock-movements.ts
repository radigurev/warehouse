import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  StockMovementDto,
  SearchStockMovementsRequest,
  RecordStockMovementRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<StockMovementDto, StockMovementDto, RecordStockMovementRequest, never, SearchStockMovementsRequest>(
  '/stock-movements',
);

export const searchMovements = base.search;
export const recordMovement = base.create;
