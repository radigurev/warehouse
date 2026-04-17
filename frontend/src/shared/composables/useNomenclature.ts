import { ref, watch, type Ref } from 'vue';
import {
  getCountries,
  getStateProvinces,
  getCities,
  getCurrencies,
} from '@shared/api/nomenclature';
import type {
  CountryDto,
  StateProvinceDto,
  CityDto,
  CurrencyDto,
} from '@shared/types/nomenclature';

const countriesCache = ref<CountryDto[] | null>(null);
const currenciesCache = ref<CurrencyDto[] | null>(null);

export function useCountries() {
  const countries = ref<CountryDto[]>([]);
  const loading = ref(false);
  const error = ref(false);

  async function load(): Promise<void> {
    if (countriesCache.value) {
      countries.value = countriesCache.value;
      return;
    }
    loading.value = true;
    error.value = false;
    try {
      const data = await getCountries();
      countriesCache.value = data;
      countries.value = data;
    } catch {
      error.value = true;
    } finally {
      loading.value = false;
    }
  }

  function refresh(): void {
    countriesCache.value = null;
    load();
  }

  load();

  return { countries, loading, error, refresh };
}

export function useStateProvinces(countryId: Ref<number | null>) {
  const stateProvinces = ref<StateProvinceDto[]>([]);
  const loading = ref(false);
  const error = ref(false);

  watch(
    countryId,
    async (id) => {
      stateProvinces.value = [];
      error.value = false;
      if (!id) return;
      loading.value = true;
      try {
        stateProvinces.value = await getStateProvinces(id);
      } catch {
        error.value = true;
      } finally {
        loading.value = false;
      }
    },
    { immediate: true },
  );

  return { stateProvinces, loading, error };
}

export function useCities(stateProvinceId: Ref<number | null>) {
  const cities = ref<CityDto[]>([]);
  const loading = ref(false);
  const error = ref(false);

  watch(
    stateProvinceId,
    async (id) => {
      cities.value = [];
      error.value = false;
      if (!id) return;
      loading.value = true;
      try {
        cities.value = await getCities(id);
      } catch {
        error.value = true;
      } finally {
        loading.value = false;
      }
    },
    { immediate: true },
  );

  return { cities, loading, error };
}

export function useCurrencies() {
  const currencies = ref<CurrencyDto[]>([]);
  const loading = ref(false);
  const error = ref(false);

  async function load(): Promise<void> {
    if (currenciesCache.value) {
      currencies.value = currenciesCache.value;
      return;
    }
    loading.value = true;
    error.value = false;
    try {
      const data = await getCurrencies();
      currenciesCache.value = data;
      currencies.value = data;
    } catch {
      error.value = true;
    } finally {
      loading.value = false;
    }
  }

  function refresh(): void {
    currenciesCache.value = null;
    load();
  }

  load();

  return { currencies, loading, error, refresh };
}
