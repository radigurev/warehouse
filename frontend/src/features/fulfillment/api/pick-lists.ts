import apiClient from '@shared/api/client';
import type {
  PickListDto,
  PickListDetailDto,
  CreatePickListRequest,
  PickLineRequest,
  SearchPickListsRequest,
} from '@features/fulfillment/types/fulfillment';
import type { PaginatedResponse } from '@shared/types/api';

export function searchPickLists(params: SearchPickListsRequest): Promise<PaginatedResponse<PickListDto>> {
  return apiClient.get<PaginatedResponse<PickListDto>>('/pick-lists', { params }).then((r) => r.data);
}

export function getPickListById(id: number): Promise<PickListDetailDto> {
  return apiClient.get<PickListDetailDto>(`/pick-lists/${id}`).then((r) => r.data);
}

export function generatePickList(request: CreatePickListRequest): Promise<PickListDetailDto> {
  return apiClient.post<PickListDetailDto>('/pick-lists', request).then((r) => r.data);
}

export function cancelPickList(id: number): Promise<void> {
  return apiClient.post(`/pick-lists/${id}/cancel`, {}).then(() => undefined);
}

export function pickLine(pickListId: number, lineId: number, request: PickLineRequest): Promise<void> {
  return apiClient.post(`/pick-lists/${pickListId}/lines/${lineId}/pick`, request).then(() => undefined);
}
