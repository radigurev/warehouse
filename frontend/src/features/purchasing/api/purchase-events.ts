import { createCrudApi } from '@shared/api/createCrudApi';
import type {
  PurchaseEventDto,
  SearchPurchaseEventsRequest,
} from '@features/purchasing/types/purchasing';

const base = createCrudApi<PurchaseEventDto, PurchaseEventDto, never, never, SearchPurchaseEventsRequest>(
  '/purchase-events',
);

export const searchPurchaseEvents = base.search;
