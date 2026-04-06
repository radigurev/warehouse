<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="600" :title="isEdit ? t('warehouses.edit') : t('warehouses.create')" :icon="isEdit ? 'mdi-warehouse' : 'mdi-warehouse'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('warehouses.edit') : t('warehouses.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('warehouses.form.name')"
              prepend-inner-icon="mdi-warehouse"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
              :error-messages="fieldErrors.name"
              @update:model-value="fieldErrors.name = []"
            />
          </v-col>

          <v-col v-if="!isEdit" v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.code"
              :label="t('warehouses.form.code')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.codeFormat, rules.codeLength]"
              :error-messages="fieldErrors.code"
              @update:model-value="fieldErrors.code = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.address"
              :label="t('warehouses.form.address')"
              prepend-inner-icon="mdi-map-marker"
              :density="layout.vuetifyDensity"
              :rules="[rules.addressLength]"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('warehouses.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.notesLength]"
              rows="3"
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
import { createWarehouse, updateWarehouse, getWarehouseById } from '@features/inventory/api/warehouses';
import type { WarehouseDto } from '@features/inventory/types/inventory';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  warehouse?: WarehouseDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
  back: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({
  name: [],
  code: [],
});

const form = reactive({
  name: '',
  code: '',
  address: '',
  notes: '',
});

async function populateForm(): Promise<void> {
  if (visible.value && props.warehouse) {
    isEdit.value = true;
    try {
      const detail = await getWarehouseById(props.warehouse.id);
      form.name = detail.name;
      form.code = detail.code;
      form.address = detail.address ?? '';
      form.notes = detail.notes ?? '';
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.name = '';
    form.code = '';
    form.address = '';
    form.notes = '';
  }
  fieldErrors.name = [];
  fieldErrors.code = [];
}

watch(visible, populateForm);
watch(() => props.warehouse, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  nameLength: (v: string) => !v || v.length <= 200 || t('validation.customerNameLength'),
  codeFormat: (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || t('validation.codeFormat'),
  codeLength: (v: string) => !v || v.length <= 20 || t('validation.codeLength'),
  addressLength: (v: string) => !v || v.length <= 500 || t('validation.descriptionLength'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.warehouse) {
      await updateWarehouse(props.warehouse.id, {
        name: form.name,
        address: form.address || null,
        notes: form.notes || null,
      });
      notification.success(t('warehouses.edit'));
    } else {
      await createWarehouse({
        code: form.code,
        name: form.name,
        address: form.address || null,
        notes: form.notes || null,
      });
      notification.success(t('warehouses.create'));
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
  if (errorCode === 'DUPLICATE_WAREHOUSE_CODE') {
    fieldErrors.code = [err.response?.data?.detail || t('errors.DUPLICATE_WAREHOUSE_CODE')];
  } else if (errorCode) {
    const key = `errors.${errorCode}`;
    const translated = t(key);
    notification.error(translated !== key ? translated : t('errors.UNEXPECTED_ERROR'));
  } else {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
