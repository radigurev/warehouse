import apiClient from '@shared/api/client';
import type {
  StocktakeSessionDto,
  StocktakeSessionDetailDto,
  StocktakeCountDto,
  InventoryAdjustmentDetailDto,
  CreateStocktakeSessionRequest,
  SearchStocktakeSessionsRequest,
  RecordStocktakeCountRequest,
  UpdateStocktakeCountRequest,
} from '@features/inventory/types/inventory';
import type { PaginatedResponse } from '@shared/types/api';

export function searchSessions(params: SearchStocktakeSessionsRequest): Promise<PaginatedResponse<StocktakeSessionDto>> {
  return apiClient.get<PaginatedResponse<StocktakeSessionDto>>('/stocktake', { params }).then((r) => r.data);
}

export function getSessionById(id: number): Promise<StocktakeSessionDetailDto> {
  return apiClient.get<StocktakeSessionDetailDto>(`/stocktake/${id}`).then((r) => r.data);
}

export function createSession(request: CreateStocktakeSessionRequest): Promise<StocktakeSessionDetailDto> {
  return apiClient.post<StocktakeSessionDetailDto>('/stocktake', request).then((r) => r.data);
}

export function startSession(id: number): Promise<StocktakeSessionDetailDto> {
  return apiClient.post<StocktakeSessionDetailDto>(`/stocktake/${id}/start`, {}).then((r) => r.data);
}

export function completeSession(id: number): Promise<StocktakeSessionDetailDto> {
  return apiClient.post<StocktakeSessionDetailDto>(`/stocktake/${id}/complete`, {}).then((r) => r.data);
}

export function cancelSession(id: number): Promise<StocktakeSessionDetailDto> {
  return apiClient.post<StocktakeSessionDetailDto>(`/stocktake/${id}/cancel`, {}).then((r) => r.data);
}

export function getVarianceReport(id: number): Promise<StocktakeCountDto[]> {
  return apiClient.get<StocktakeCountDto[]>(`/stocktake/${id}/variance`).then((r) => r.data);
}

export function createAdjustmentFromSession(id: number): Promise<InventoryAdjustmentDetailDto> {
  return apiClient.post<InventoryAdjustmentDetailDto>(`/stocktake/${id}/create-adjustment`, {}).then((r) => r.data);
}

export function addCount(sessionId: number, request: RecordStocktakeCountRequest): Promise<StocktakeCountDto> {
  return apiClient.post<StocktakeCountDto>(`/stocktake/${sessionId}/counts`, request).then((r) => r.data);
}

export function updateCount(sessionId: number, countId: number, request: UpdateStocktakeCountRequest): Promise<StocktakeCountDto> {
  return apiClient.put<StocktakeCountDto>(`/stocktake/${sessionId}/counts/${countId}`, request).then((r) => r.data);
}

export function deleteCount(sessionId: number, countId: number): Promise<void> {
  return apiClient.delete(`/stocktake/${sessionId}/counts/${countId}`).then(() => undefined);
}

export function listCounts(sessionId: number): Promise<StocktakeCountDto[]> {
  return apiClient.get<StocktakeCountDto[]>(`/stocktake/${sessionId}/counts`).then((r) => r.data);
}
