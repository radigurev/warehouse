import apiClient from '@shared/api/client';
import type { FulfillmentEventDto, SearchFulfillmentEventsRequest } from '@features/fulfillment/types/fulfillment';
import type { PaginatedResponse } from '@shared/types/api';

export function searchFulfillmentEvents(params: SearchFulfillmentEventsRequest): Promise<PaginatedResponse<FulfillmentEventDto>> {
  return apiClient.get<PaginatedResponse<FulfillmentEventDto>>('/fulfillment-events', { params }).then((r) => r.data);
}
