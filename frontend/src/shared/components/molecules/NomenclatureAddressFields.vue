<template>
  <v-autocomplete
    v-if="!countryError"
    :model-value="countryCode"
    :items="countryItems"
    item-title="name"
    item-value="iso2Code"
    :label="t('nomenclature.countryPlaceholder')"
    :loading="countriesLoading"
    :disabled="disabled"
    :readonly="readonly"
    :density="density"
    prepend-inner-icon="mdi-flag"
    clearable
    auto-select-first
    @update:model-value="onCountryChange"
  />
  <v-text-field
    v-else
    :model-value="countryCode"
    :label="t('nomenclature.countryPlaceholder')"
    :hint="t('nomenclature.loadingError')"
    persistent-hint
    :disabled="disabled"
    :readonly="readonly"
    :density="density"
    @update:model-value="$emit('update:countryCode', $event)"
  />

  <v-autocomplete
    v-if="showStateDropdown"
    :model-value="stateProvince"
    :items="stateProvinceItems"
    item-title="name"
    item-value="name"
    :label="t('nomenclature.stateProvincePlaceholder')"
    :loading="stateProvincesLoading"
    :disabled="disabled || !countryCode"
    :readonly="readonly"
    :density="density"
    prepend-inner-icon="mdi-map"
    clearable
    auto-select-first
    @update:model-value="onStateProvinceChange"
  />
  <v-text-field
    v-else
    :model-value="stateProvince"
    :label="t('nomenclature.stateProvincePlaceholder')"
    :hint="stateProvinceFallbackHint"
    :persistent-hint="!!stateProvinceFallbackHint"
    :disabled="disabled || (!countryCode && !countryError)"
    :readonly="readonly"
    :density="density"
    @update:model-value="onStateProvinceChange"
  />

  <v-autocomplete
    v-if="showCityDropdown"
    :model-value="city"
    :items="cityItems"
    item-title="name"
    item-value="name"
    :label="t('nomenclature.cityPlaceholder')"
    :loading="citiesLoading"
    :disabled="disabled || !stateProvince"
    :readonly="readonly"
    :density="density"
    prepend-inner-icon="mdi-city"
    clearable
    auto-select-first
    @update:model-value="$emit('update:city', $event)"
  />
  <v-text-field
    v-else
    :model-value="city"
    :label="t('nomenclature.cityPlaceholder')"
    :hint="cityFallbackHint"
    :persistent-hint="!!cityFallbackHint"
    :disabled="disabled || (!stateProvince && !countryError)"
    :readonly="readonly"
    :density="density"
    @update:model-value="$emit('update:city', $event)"
  />
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useCountries, useStateProvinces, useCities } from '@shared/composables/useNomenclature';

const { t } = useI18n();

const props = defineProps<{
  countryCode: string;
  stateProvince: string;
  city: string;
  disabled?: boolean;
  readonly?: boolean;
  density?: 'default' | 'comfortable' | 'compact';
}>();

const emit = defineEmits<{
  'update:countryCode': [value: string];
  'update:stateProvince': [value: string];
  'update:city': [value: string];
}>();

const { countries, loading: countriesLoading, error: countryError } = useCountries();

const selectedCountryId = ref<number | null>(null);
const selectedStateProvinceId = ref<number | null>(null);

const countryItems = computed(() => countries.value);

const {
  stateProvinces,
  loading: stateProvincesLoading,
  error: stateProvinceError,
} = useStateProvinces(selectedCountryId);

const {
  cities,
  loading: citiesLoading,
  error: cityError,
} = useCities(selectedStateProvinceId);

const stateProvinceItems = computed(() => stateProvinces.value);
const cityItems = computed(() => cities.value);

const showStateDropdown = computed(() => {
  if (countryError.value || stateProvinceError.value) return false;
  if (!selectedCountryId.value) return false;
  if (!stateProvincesLoading.value && stateProvinces.value.length === 0) return false;
  return true;
});

const showCityDropdown = computed(() => {
  if (countryError.value || stateProvinceError.value || cityError.value) return false;
  if (!selectedStateProvinceId.value) return false;
  if (!citiesLoading.value && cities.value.length === 0) return false;
  return true;
});

const stateProvinceFallbackHint = computed(() => {
  if (countryError.value || stateProvinceError.value) return t('nomenclature.loadingError');
  if (selectedCountryId.value && !stateProvincesLoading.value && stateProvinces.value.length === 0)
    return t('nomenclature.noStateProvinces');
  return '';
});

const cityFallbackHint = computed(() => {
  if (countryError.value || stateProvinceError.value || cityError.value)
    return t('nomenclature.loadingError');
  if (
    selectedStateProvinceId.value &&
    !citiesLoading.value &&
    cities.value.length === 0
  )
    return t('nomenclature.noCities');
  return '';
});

function resolveCountryId(iso2Code: string | null): number | null {
  if (!iso2Code) return null;
  const match = countries.value.find((c) => c.iso2Code === iso2Code);
  return match?.id ?? null;
}

function resolveStateProvinceId(name: string | null): number | null {
  if (!name) return null;
  const match = stateProvinces.value.find((sp) => sp.name === name);
  return match?.id ?? null;
}

watch(
  () => countries.value,
  () => {
    selectedCountryId.value = resolveCountryId(props.countryCode);
  },
);

watch(
  () => stateProvinces.value,
  () => {
    selectedStateProvinceId.value = resolveStateProvinceId(props.stateProvince);
  },
);

watch(
  () => props.countryCode,
  (code) => {
    selectedCountryId.value = resolveCountryId(code);
  },
  { immediate: true },
);

watch(
  () => props.stateProvince,
  (name) => {
    selectedStateProvinceId.value = resolveStateProvinceId(name);
  },
  { immediate: true },
);

function onCountryChange(value: string | null): void {
  emit('update:countryCode', value ?? '');
  emit('update:stateProvince', '');
  emit('update:city', '');
  selectedCountryId.value = resolveCountryId(value ?? null);
  selectedStateProvinceId.value = null;
}

function onStateProvinceChange(value: string | null): void {
  emit('update:stateProvince', value ?? '');
  emit('update:city', '');
  selectedStateProvinceId.value = resolveStateProvinceId(value ?? null);
}

// TODO: CHG-ENH-001 S2.1.7 — Add inline "Add new" support for admin users
// with `nomenclature:create` permission. Requires reading from auth store.
</script>
