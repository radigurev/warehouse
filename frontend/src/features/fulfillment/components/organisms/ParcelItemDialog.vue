<template>
  <FormWrapper v-model="visible" max-width="500" :title="t('salesOrders.parcel.addItem')" icon="mdi-plus-box">
    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-select
          v-model="form.productId"
          :label="t('salesOrders.lines.product')"
          :density="layout.vuetifyDensity"
          :items="productItems"
          item-title="label"
          item-value="productId"
          prepend-inner-icon="mdi-package-variant"
          :rules="[rules.requiredSelect]"
        />
        <v-select
          v-model="form.pickListLineId"
          :label="t('salesOrders.parcel.pickListLine')"
          :density="layout.vuetifyDensity"
          :items="filteredPickLines"
          item-title="label"
          item-value="id"
          prepend-inner-icon="mdi-clipboard-list"
          :rules="[rules.requiredSelect]"
          :disabled="!form.productId"
        />
        <v-text-field
          v-model.number="form.quantity"
          :label="t('salesOrders.parcel.quantity')"
          :density="layout.vuetifyDensity"
          type="number"
          :min="0.01"
          step="any"
          prepend-inner-icon="mdi-counter"
          :rules="[rules.positiveNumber]"
        />
      </v-form>
    </v-card-text>
    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="visible = false">{{ t('common.cancel') }}</v-btn>
      <v-btn color="primary" variant="flat" :loading="loading" @click="handleSubmit">
        {{ t('common.save') }}
      </v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import type { SalesOrderLineDto, PickListLineDto } from '@features/fulfillment/types/fulfillment';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  soLines: SalesOrderLineDto[];
  pickLines: PickListLineDto[];
  salesOrderId?: number;
  parcelId?: number;
}>();

const emit = defineEmits<{
  saved: [request: { productId: number; pickListLineId: number; quantity: number }];
}>();

const formRef = ref();
const loading = ref(false);

const form = reactive({
  productId: null as number | null,
  pickListLineId: null as number | null,
  quantity: 1,
});

const productItems = computed(() => {
  const seen = new Set<number>();
  return props.soLines
    .filter((l) => {
      if (seen.has(l.productId)) return false;
      seen.add(l.productId);
      return true;
    })
    .map((l) => ({
      productId: l.productId,
      label: `${l.productCode} - ${l.productName}`,
    }));
});

const filteredPickLines = computed(() => {
  if (!form.productId) return [];
  return props.pickLines
    .filter((pl) => pl.productId === form.productId && pl.status === 'Picked')
    .map((pl) => ({
      id: pl.id,
      label: `${pl.sourceLocationCode || 'N/A'} - Picked: ${pl.actualPickedQuantity}`,
    }));
});

watch(visible, (val) => {
  if (val) {
    form.productId = null;
    form.pickListLineId = null;
    form.quantity = 1;
  }
});

watch(() => form.productId, () => {
  form.pickListLineId = null;
});

const rules = {
  requiredSelect: (v: number | null) => v !== null || t('common.required'),
  positiveNumber: (v: number) => v > 0 || t('validation.mustBePositive'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid || !form.productId || !form.pickListLineId) return;

  loading.value = true;
  try {
    emit('saved', {
      productId: form.productId,
      pickListLineId: form.pickListLineId,
      quantity: form.quantity,
    });
    visible.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}
</script>
