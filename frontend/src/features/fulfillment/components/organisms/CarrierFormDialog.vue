<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="600" :title="isEdit ? t('carriers.edit') : t('carriers.create')" :icon="isEdit ? 'mdi-truck-delivery' : 'mdi-truck-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('carriers.edit') : t('carriers.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-if="!isEdit" v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.code"
              :label="t('carriers.form.code')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.codeFormat, rules.codeLength]"
              :error-messages="fieldErrors.code"
              @update:model-value="fieldErrors.code = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('carriers.form.name')"
              prepend-inner-icon="mdi-truck-delivery"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.contactPhone"
              :label="t('carriers.form.contactPhone')"
              prepend-inner-icon="mdi-phone"
              :density="layout.vuetifyDensity"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.contactEmail"
              :label="t('carriers.form.contactEmail')"
              prepend-inner-icon="mdi-email"
              :density="layout.vuetifyDensity"
              :rules="[rules.emailFormat]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.websiteUrl"
              :label="t('carriers.form.websiteUrl')"
              prepend-inner-icon="mdi-web"
              :density="layout.vuetifyDensity"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.trackingUrlTemplate"
              :label="t('carriers.form.trackingUrlTemplate')"
              prepend-inner-icon="mdi-link-variant"
              :density="layout.vuetifyDensity"
              :hint="t('carriers.form.trackingUrlHint')"
              persistent-hint
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('carriers.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.notesLength]"
              rows="2"
              auto-grow
            />
          </v-col>
        </v-row>
      </v-form>
    </v-card-text>

    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="cancel">{{ t('common.cancel') }}</v-btn>
      <v-btn color="primary" variant="flat" :loading="loading" @click="handleSubmit">
        {{ t('common.save') }}
      </v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { createCarrier, updateCarrier, getCarrierById } from '@features/fulfillment/api/carriers';
import type { CarrierDetailDto } from '@features/fulfillment/types/fulfillment';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  carrierId?: number | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
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
  contactPhone: '',
  contactEmail: '',
  websiteUrl: '',
  trackingUrlTemplate: '',
  notes: '',
});

async function populateForm(): Promise<void> {
  if (visible.value && props.carrierId) {
    isEdit.value = true;
    try {
      const detail: CarrierDetailDto = await getCarrierById(props.carrierId);
      form.code = detail.code;
      form.name = detail.name;
      form.contactPhone = detail.contactPhone ?? '';
      form.contactEmail = detail.contactEmail ?? '';
      form.websiteUrl = detail.websiteUrl ?? '';
      form.trackingUrlTemplate = detail.trackingUrlTemplate ?? '';
      form.notes = detail.notes ?? '';
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.code = '';
    form.name = '';
    form.contactPhone = '';
    form.contactEmail = '';
    form.websiteUrl = '';
    form.trackingUrlTemplate = '';
    form.notes = '';
  }
  fieldErrors.code = [];
}

watch(visible, populateForm);
watch(() => props.carrierId, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  codeFormat: (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || t('validation.codeFormat'),
  codeLength: (v: string) => !v || v.length <= 20 || t('validation.codeLength'),
  nameLength: (v: string) => !v || v.length <= 200 || t('validation.nameLength'),
  emailFormat: (v: string) => !v || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) || t('validation.emailInvalid'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.carrierId) {
      await updateCarrier(props.carrierId, {
        name: form.name,
        contactPhone: form.contactPhone || null,
        contactEmail: form.contactEmail || null,
        websiteUrl: form.websiteUrl || null,
        trackingUrlTemplate: form.trackingUrlTemplate || null,
        notes: form.notes || null,
      });
      notification.success(t('carriers.edit') + ' \u2713');
    } else {
      await createCarrier({
        code: form.code,
        name: form.name,
        contactPhone: form.contactPhone || null,
        contactEmail: form.contactEmail || null,
        websiteUrl: form.websiteUrl || null,
        trackingUrlTemplate: form.trackingUrlTemplate || null,
        notes: form.notes || null,
      });
      notification.success(t('carriers.create') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch (err) {
    handleApiError(err as AxiosError<ProblemDetails>);
  } finally {
    loading.value = false;
  }
}

function handleApiError(err: AxiosError<ProblemDetails>): void {
  const errorCode = err.response?.data?.title;
  if (errorCode === 'DUPLICATE_CARRIER_CODE') {
    fieldErrors.code = [err.response?.data?.detail || t('errors.DUPLICATE_CARRIER_CODE')];
  } else {
    notification.error(getApiErrorMessage(err, t));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
