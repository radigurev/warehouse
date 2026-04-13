import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  BatchDto,
  CreateBatchRequest,
  UpdateBatchRequest,
  SearchBatchesRequest,
} from '@features/inventory/types/inventory';

const base = createCrudApi<BatchDto, BatchDto, CreateBatchRequest, UpdateBatchRequest, SearchBatchesRequest>(
  '/batches',
);

export const searchBatches = base.search;
export const getBatchById = base.getById;
export const createBatch = base.create;
export const updateBatch = base.update;
export const deactivateBatch = base.remove;
