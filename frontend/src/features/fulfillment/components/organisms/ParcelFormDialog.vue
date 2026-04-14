<template>
  <FormWrapper v-model="visible" max-width="500" :title="isEdit ? t('salesOrders.parcel.edit') : t('salesOrders.parcel.create')" icon="mdi-package-variant">
    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-text-field
          v-model.number="form.weightKg"
          :label="t('salesOrders.parcel.weight')"
          :density="layout.vuetifyDensity"
          type="number"
          :min="0"
          step="any"
          prepend-inner-icon="mdi-weight"
          clearable
        />
        <v-row dense>
          <v-col cols="4">
            <v-text-field
              v-model.number="form.lengthCm"
              :label="t('salesOrders.parcel.length')"
              :density="layout.vuetifyDensity"
              type="number"
              :min="0"
              step="any"
              clearable
            />
          </v-col>
          <v-col cols="4">
            <v-text-field
              v-model.number="form.widthCm"
              :label="t('salesOrders.parcel.width')"
              :density="layout.vuetifyDensity"
              type="number"
              :min="0"
              step="any"
              clearable
            />
          </v-col>
          <v-col cols="4">
            <v-text-field
              v-model.number="form.heightCm"
              :label="t('salesOrders.parcel.height')"
              :density="layout.vuetifyDensity"
              type="number"
              :min="0"
              step="any"
              clearable
            />
          </v-col>
        </v-row>
        <v-text-field
          v-model="form.trackingNumber"
          :label="t('salesOrders.parcel.tracking')"
          :density="layout.vuetifyDensity"
          prepend-inner-icon="mdi-barcode"
          clearable
        />
        <v-textarea
          v-model="form.notes"
          :label="t('salesOrders.parcel.notes')"
          :density="layout.vuetifyDensity"
          rows="2"
          auto-grow
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
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { createParcel, updateParcel } from '@features/fulfillment/api/parcels';
import type { ParcelDto } from '@features/fulfillment/types/fulfillment';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  parcel?: ParcelDto | null;
  salesOrderId?: number;
}>();

const emit = defineEmits<{
  saved: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);

const form = reactive({
  weightKg: null as number | null,
  lengthCm: null as number | null,
  widthCm: null as number | null,
  heightCm: null as number | null,
  trackingNumber: '',
  notes: '',
});

watch(visible, (val) => {
  if (val && props.parcel) {
    isEdit.value = true;
    form.weightKg = props.parcel.weightKg;
    form.lengthCm = props.parcel.lengthCm;
    form.widthCm = props.parcel.widthCm;
    form.heightCm = props.parcel.heightCm;
    form.trackingNumber = props.parcel.trackingNumber ?? '';
    form.notes = props.parcel.notes ?? '';
  } else if (val) {
    isEdit.value = false;
    form.weightKg = null;
    form.lengthCm = null;
    form.widthCm = null;
    form.heightCm = null;
    form.trackingNumber = '';
    form.notes = '';
  }
});

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    const payload = {
      weightKg: form.weightKg,
      lengthCm: form.lengthCm,
      widthCm: form.widthCm,
      heightCm: form.heightCm,
      trackingNumber: form.trackingNumber || null,
      notes: form.notes || null,
    };

    if (isEdit.value && props.parcel && props.salesOrderId) {
      await updateParcel(props.salesOrderId, props.parcel.id, payload);
      notification.success(t('salesOrders.parcel.edit') + ' \u2713');
    } else if (props.salesOrderId) {
      await createParcel(props.salesOrderId, payload);
      notification.success(t('salesOrders.parcel.create') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}
</script>
