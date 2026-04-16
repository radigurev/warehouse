<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="600" :title="isEdit ? t('suppliers.edit') : t('suppliers.create')" :icon="isEdit ? 'mdi-truck-check' : 'mdi-truck-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('suppliers.edit') : t('suppliers.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('suppliers.form.name')"
              prepend-inner-icon="mdi-truck-delivery"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
              :error-messages="fieldErrors.name"
              @update:model-value="fieldErrors.name = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.code"
              :label="t('suppliers.form.code')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
              readonly
              :loading="codeLoading"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.taxId"
              :label="t('suppliers.form.taxId')"
              prepend-inner-icon="mdi-card-account-details"
              :density="layout.vuetifyDensity"
              :rules="[rules.taxIdLength]"
              :error-messages="fieldErrors.taxId"
              @update:model-value="fieldErrors.taxId = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.categoryId"
              :label="t('suppliers.form.category')"
              prepend-inner-icon="mdi-tag"
              :density="layout.vuetifyDensity"
              :items="categories"
              item-title="name"
              item-value="id"
              clearable
              :loading="categoriesLoading"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model.number="form.paymentTermDays"
              :label="t('suppliers.form.paymentTermDays')"
              prepend-inner-icon="mdi-calendar-clock"
              :density="layout.vuetifyDensity"
              type="number"
              :min="0"
              :max="365"
              :rules="[rules.paymentTermRange]"
              clearable
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('suppliers.form.notes')"
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
import { createSupplier, updateSupplier, getSupplierById, getNextSupplierCode } from '@features/purchasing/api/suppliers';
import { getAllSupplierCategories } from '@features/purchasing/api/supplier-categories';
import type { SupplierDto, SupplierCategoryDto } from '@features/purchasing/types/purchasing';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  supplierId?: number | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const categories = ref<SupplierCategoryDto[]>([]);
const categoriesLoading = ref(false);
const codeLoading = ref(false);

const fieldErrors = reactive<Record<string, string[]>>({
  name: [],
  code: [],
  taxId: [],
});

const form = reactive({
  name: '',
  code: '',
  taxId: '',
  categoryId: null as number | null,
  paymentTermDays: null as number | null,
  notes: '',
});

async function loadCategories(): Promise<void> {
  categoriesLoading.value = true;
  try {
    categories.value = await getAllSupplierCategories();
  } catch {
    // silent
  } finally {
    categoriesLoading.value = false;
  }
}

onMounted(() => loadCategories());

async function populateForm(): Promise<void> {
  if (visible.value && props.supplierId) {
    isEdit.value = true;
    try {
      const detail = await getSupplierById(props.supplierId);
      form.name = detail.name;
      form.code = detail.code;
      form.taxId = detail.taxId ?? '';
      form.categoryId = detail.categoryId;
      form.paymentTermDays = detail.paymentTermDays;
      form.notes = detail.notes ?? '';
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.name = '';
    form.code = '';
    form.taxId = '';
    form.categoryId = null;
    form.paymentTermDays = null;
    form.notes = '';
    codeLoading.value = true;
    try {
      form.code = await getNextSupplierCode();
    } catch {
      form.code = '';
    } finally {
      codeLoading.value = false;
    }
  }
  fieldErrors.name = [];
  fieldErrors.code = [];
  fieldErrors.taxId = [];
}

watch(visible, populateForm);
watch(() => props.supplierId, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  nameLength: (v: string) => !v || v.length <= 200 || t('validation.supplierNameLength'),
  codeFormat: (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || t('validation.codeFormat'),
  codeLength: (v: string) => !v || v.length <= 50 || t('validation.codeLength'),
  taxIdLength: (v: string) => !v || v.length <= 50 || t('validation.taxIdLength'),
  paymentTermRange: (v: number | null) => v === null || v === undefined || (v >= 0 && v <= 365) || t('validation.paymentTermRange'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.supplierId) {
      await updateSupplier(props.supplierId, {
        name: form.name,
        taxId: form.taxId || null,
        categoryId: form.categoryId,
        paymentTermDays: form.paymentTermDays,
        notes: form.notes || null,
      });
      notification.success(t('suppliers.edit') + ' \u2713');
    } else {
      await createSupplier({
        name: form.name,
        code: form.code || null,
        taxId: form.taxId || null,
        categoryId: form.categoryId,
        paymentTermDays: form.paymentTermDays,
        notes: form.notes || null,
      });
      notification.success(t('suppliers.create') + ' \u2713');
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
  if (errorCode === 'DUPLICATE_SUPPLIER_CODE') {
    fieldErrors.code = [err.response?.data?.detail || t('errors.DUPLICATE_SUPPLIER_CODE')];
  } else if (errorCode === 'DUPLICATE_TAX_ID') {
    fieldErrors.taxId = [err.response?.data?.detail || t('errors.DUPLICATE_TAX_ID')];
  } else {
    notification.error(getApiErrorMessage(err, t));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
