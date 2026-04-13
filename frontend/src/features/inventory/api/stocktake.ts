import apiClient from '@shared/api/client';
import { createCrudApi } from '@shared/api/createCrudApi';
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

const base = createCrudApi<StocktakeSessionDto, StocktakeSessionDetailDto, CreateStocktakeSessionRequest, never, SearchStocktakeSessionsRequest>(
  '/stocktake',
);

export const searchSessions = base.search;
export const getSessionById = base.getById;
export const createSession = base.create;

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
  return apiClient.get<StocktakeCountDto[]>(`/stocktake/${id}/variance-report`).then((r) => r.data);
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
