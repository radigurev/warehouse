import apiClient from '@shared/api/client';
import type {
  PurchaseEventDto,
  SearchPurchaseEventsRequest,
} from '@features/purchasing/types/purchasing';
import type { PaginatedResponse } from '@shared/types/api';

export function searchPurchaseEvents(params: SearchPurchaseEventsRequest): Promise<PaginatedResponse<PurchaseEventDto>> {
  return apiClient.get<PaginatedResponse<PurchaseEventDto>>('/purchase-events', { params }).then((r) => r.data);
}
