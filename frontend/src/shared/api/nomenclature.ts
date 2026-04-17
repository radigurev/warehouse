import apiClient from '@shared/api/client';
import type {
  CountryDto,
  StateProvinceDto,
  CityDto,
  CurrencyDto,
} from '@shared/types/nomenclature';

export function getCountries(): Promise<CountryDto[]> {
  return apiClient.get<CountryDto[]>('/countries').then((r) => r.data);
}

export function getStateProvinces(countryId: number): Promise<StateProvinceDto[]> {
  return apiClient
    .get<StateProvinceDto[]>('/state-provinces', { params: { countryId } })
    .then((r) => r.data);
}

export function getCities(stateProvinceId: number): Promise<CityDto[]> {
  return apiClient
    .get<CityDto[]>('/cities', { params: { stateProvinceId } })
    .then((r) => r.data);
}

export function getCurrencies(): Promise<CurrencyDto[]> {
  return apiClient.get<CurrencyDto[]>('/currencies').then((r) => r.data);
}

export function createCountry(request: {
  iso2Code: string;
  iso3Code: string;
  name: string;
  phonePrefix?: string | null;
}): Promise<CountryDto> {
  return apiClient.post<CountryDto>('/countries', request).then((r) => r.data);
}

export function createStateProvince(request: {
  name: string;
  countryId: number;
}): Promise<StateProvinceDto> {
  return apiClient.post<StateProvinceDto>('/state-provinces', request).then((r) => r.data);
}

export function createCity(request: {
  name: string;
  postalCode?: string | null;
  stateProvinceId: number;
}): Promise<CityDto> {
  return apiClient.post<CityDto>('/cities', request).then((r) => r.data);
}
