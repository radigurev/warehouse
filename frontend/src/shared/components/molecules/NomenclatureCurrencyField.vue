<template>
  <v-autocomplete
    v-if="!error"
    :model-value="modelValue"
    :items="currencyItems"
    :item-title="formatCurrencyLabel"
    item-value="code"
    :label="t('nomenclature.currencyPlaceholder')"
    :loading="loading"
    :disabled="disabled"
    :readonly="readonly"
    :density="density"
    clearable
    auto-select-first
    @update:model-value="$emit('update:modelValue', $event ?? '')"
  />
  <v-text-field
    v-else
    :model-value="modelValue"
    :label="t('nomenclature.currencyPlaceholder')"
    :hint="t('nomenclature.loadingError')"
    persistent-hint
    :disabled="disabled"
    :readonly="readonly"
    :density="density"
    @update:model-value="$emit('update:modelValue', $event)"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useCurrencies } from '@shared/composables/useNomenclature';
import type { CurrencyDto } from '@shared/types/nomenclature';

const { t } = useI18n();

defineProps<{
  modelValue: string;
  disabled?: boolean;
  readonly?: boolean;
  density?: 'default' | 'comfortable' | 'compact';
}>();

defineEmits<{
  'update:modelValue': [value: string];
}>();

const { currencies, loading, error } = useCurrencies();

const currencyItems = computed(() => currencies.value);

function formatCurrencyLabel(item: CurrencyDto): string {
  return `${item.name} (${item.code})`;
}
</script>
