<template>
  <v-menu :close-on-content-click="false" location="bottom" offset="4">
    <template #activator="{ props: menuProps }">
      <v-btn
        v-bind="menuProps"
        :icon="hasActiveFilter ? 'mdi-filter' : 'mdi-filter-outline'"
        :color="hasActiveFilter ? 'primary' : undefined"
        size="x-small"
        variant="text"
        density="compact"
        class="ml-1"
      />
    </template>

    <v-card min-width="260">
      <v-card-text class="pb-2">
        <v-select
          v-model="localOperator"
          :items="operatorItems"
          :label="t('filter.operator')"
          density="compact"
          hide-details
          class="mb-3"
          prepend-inner-icon="mdi-tune"
        />
        <v-text-field
          v-model="localValue"
          :label="t('filter.value')"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
          @keyup.enter="apply"
        />
      </v-card-text>
      <v-card-actions>
        <v-btn size="small" variant="text" @click="clear">
          {{ t('filter.clear') }}
        </v-btn>
        <v-spacer />
        <v-btn size="small" color="primary" variant="flat" @click="apply">
          {{ t('filter.apply') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-menu>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import type { ColumnFilterState, FilterOperator } from '@shared/types/filter';

const { t } = useI18n();

const modelValue = defineModel<ColumnFilterState | null>({ required: true });

const localOperator = ref<FilterOperator>('contains');
const localValue = ref('');

const hasActiveFilter = computed(() => !!modelValue.value?.value);

const operatorItems = computed(() => [
  { title: t('filter.operators.contains'), value: 'contains' as FilterOperator },
  { title: t('filter.operators.startsWith'), value: 'startsWith' as FilterOperator },
  { title: t('filter.operators.endsWith'), value: 'endsWith' as FilterOperator },
  { title: t('filter.operators.equals'), value: 'equals' as FilterOperator },
  { title: t('filter.operators.notEquals'), value: 'notEquals' as FilterOperator },
]);

watch(modelValue, (val) => {
  if (val) {
    localOperator.value = val.operator;
    localValue.value = val.value;
  }
}, { immediate: true });

function apply(): void {
  modelValue.value = { operator: localOperator.value, value: localValue.value };
}

function clear(): void {
  localOperator.value = 'contains';
  localValue.value = '';
  modelValue.value = null;
}
</script>
