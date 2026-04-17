export interface CountryDto {
  id: number;
  iso2Code: string;
  iso3Code: string;
  name: string;
  phonePrefix: string | null;
  isActive: boolean;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
}

export interface StateProvinceDto {
  id: number;
  name: string;
  countryId: number;
  isActive: boolean;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
}

export interface CityDto {
  id: number;
  name: string;
  postalCode: string | null;
  stateProvinceId: number;
  isActive: boolean;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
}

export interface CurrencyDto {
  id: number;
  code: string;
  name: string;
  symbol: string | null;
  isActive: boolean;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
}
