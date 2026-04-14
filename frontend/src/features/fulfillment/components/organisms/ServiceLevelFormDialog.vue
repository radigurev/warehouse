<template>
  <FormWrapper v-model="visible" max-width="500" :title="isEdit ? t('carriers.serviceLevel.edit') : t('carriers.detail.addServiceLevel')" icon="mdi-speedometer">
    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-text-field
          v-if="!isEdit"
          v-model="form.code"
          :label="t('carriers.serviceLevel.code')"
          :density="layout.vuetifyDensity"
          prepend-inner-icon="mdi-identifier"
          :rules="[rules.required, rules.codeFormat, rules.codeLength]"
          :error-messages="fieldErrors.code"
          @update:model-value="fieldErrors.code = []"
        />
        <v-text-field
          v-model="form.name"
          :label="t('carriers.serviceLevel.name')"
          :density="layout.vuetifyDensity"
          prepend-inner-icon="mdi-label"
          :rules="[rules.required, rules.nameLength]"
        />
        <v-text-field
          v-model.number="form.estimatedDeliveryDays"
          :label="t('carriers.serviceLevel.estimatedDeliveryDays')"
          :density="layout.vuetifyDensity"
          type="number"
          :min="0"
          prepend-inner-icon="mdi-calendar-clock"
          clearable
        />
        <v-text-field
          v-model.number="form.baseRate"
          :label="t('carriers.serviceLevel.baseRate')"
          :density="layout.vuetifyDensity"
          type="number"
          :min="0"
          step="any"
          prepend-inner-icon="mdi-currency-usd"
          clearable
        />
        <v-text-field
          v-model.number="form.perKgRate"
          :label="t('carriers.serviceLevel.perKgRate')"
          :density="layout.vuetifyDensity"
          type="number"
          :min="0"
          step="any"
          prepend-inner-icon="mdi-weight-kilogram"
          clearable
        />
        <v-textarea
          v-model="form.notes"
          :label="t('carriers.serviceLevel.notes')"
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
import { createServiceLevel, updateServiceLevel } from '@features/fulfillment/api/carriers';
import type { CarrierServiceLevelDto } from '@features/fulfillment/types/fulfillment';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  carrierId: number;
  serviceLevel?: CarrierServiceLevelDto | null;
}>();

const emit = defineEmits<{
  saved: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);

const fieldErrors = reactive<Record<string, string[]>>({
  code: [],
});

const form = reactive({
  code: '',
  name: '',
  estimatedDeliveryDays: null as number | null,
  baseRate: null as number | null,
  perKgRate: null as number | null,
  notes: '',
});

watch(visible, (val) => {
  if (val && props.serviceLevel) {
    isEdit.value = true;
    form.code = props.serviceLevel.code;
    form.name = props.serviceLevel.name;
    form.estimatedDeliveryDays = props.serviceLevel.estimatedDeliveryDays;
    form.baseRate = props.serviceLevel.baseRate;
    form.perKgRate = props.serviceLevel.perKgRate;
    form.notes = props.serviceLevel.notes ?? '';
  } else if (val) {
    isEdit.value = false;
    form.code = '';
    form.name = '';
    form.estimatedDeliveryDays = null;
    form.baseRate = null;
    form.perKgRate = null;
    form.notes = '';
  }
  fieldErrors.code = [];
});

const rules = {
  required: (v: string) => !!v || t('common.required'),
  codeFormat: (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || t('validation.codeFormat'),
  codeLength: (v: string) => !v || v.length <= 20 || t('validation.codeLength'),
  nameLength: (v: string) => !v || v.length <= 200 || t('validation.nameLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.serviceLevel) {
      await updateServiceLevel(props.carrierId, props.serviceLevel.id, {
        name: form.name,
        estimatedDeliveryDays: form.estimatedDeliveryDays,
        baseRate: form.baseRate,
        perKgRate: form.perKgRate,
        notes: form.notes || null,
      });
      notification.success(t('carriers.serviceLevel.edit') + ' \u2713');
    } else {
      await createServiceLevel(props.carrierId, {
        code: form.code,
        name: form.name,
        estimatedDeliveryDays: form.estimatedDeliveryDays,
        baseRate: form.baseRate,
        perKgRate: form.perKgRate,
        notes: form.notes || null,
      });
      notification.success(t('carriers.detail.addServiceLevel') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    const axiosErr = err as AxiosError<ProblemDetails>;
    if (axiosErr.response?.data?.title === 'DUPLICATE_SERVICE_LEVEL_CODE') {
      fieldErrors.code = [axiosErr.response?.data?.detail || t('errors.DUPLICATE_SERVICE_LEVEL_CODE')];
    } else {
      notification.error(getApiErrorMessage(err, t));
    }
  } finally {
    loading.value = false;
  }
}
</script>
