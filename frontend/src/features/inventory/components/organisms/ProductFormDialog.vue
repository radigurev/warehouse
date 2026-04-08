<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="600" :title="isEdit ? t('products.edit') : t('products.create')" :icon="isEdit ? 'mdi-package-variant-closed' : 'mdi-package-variant-closed-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('products.edit') : t('products.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('products.form.name')"
              prepend-inner-icon="mdi-package-variant-closed"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
              :error-messages="fieldErrors.name"
              @update:model-value="fieldErrors.name = []"
            />
          </v-col>

          <v-col v-if="!isEdit" v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.code"
              :label="t('products.form.code')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.codeFormat, rules.codeLength]"
              :error-messages="fieldErrors.code"
              @update:model-value="fieldErrors.code = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.sku"
              :label="t('products.form.sku')"
              prepend-inner-icon="mdi-barcode"
              :density="layout.vuetifyDensity"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.barcode"
              :label="t('products.form.barcode')"
              prepend-inner-icon="mdi-barcode-scan"
              :density="layout.vuetifyDensity"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.categoryId"
              :label="t('products.form.category')"
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
            <v-select
              v-model="form.unitOfMeasureId"
              :label="t('products.form.unitOfMeasure')"
              prepend-inner-icon="mdi-ruler"
              :density="layout.vuetifyDensity"
              :items="units"
              item-title="name"
              item-value="id"
              :rules="[rules.requiredSelect]"
              :loading="unitsLoading"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.description"
              :label="t('products.form.description')"
              prepend-inner-icon="mdi-text"
              :density="layout.vuetifyDensity"
              rows="2"
              auto-grow
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('products.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.notesLength]"
              rows="2"
              auto-grow
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-switch
              v-model="form.requiresBatchTracking"
              :label="t('products.form.requiresBatchTracking')"
              :density="layout.vuetifyDensity"
              color="primary"
              hide-details
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
import { createProduct, updateProduct, getProductById } from '@features/inventory/api/products';
import { getAllCategories } from '@features/inventory/api/product-categories';
import { getAllUnits } from '@features/inventory/api/units-of-measure';
import type { ProductDto, ProductCategoryDto, UnitOfMeasureDto } from '@features/inventory/types/inventory';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  product?: ProductDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const categories = ref<ProductCategoryDto[]>([]);
const categoriesLoading = ref(false);
const units = ref<UnitOfMeasureDto[]>([]);
const unitsLoading = ref(false);
const fieldErrors = reactive<Record<string, string[]>>({
  name: [],
  code: [],
});

const form = reactive({
  name: '',
  code: '',
  sku: '',
  barcode: '',
  categoryId: null as number | null,
  unitOfMeasureId: null as number | null,
  description: '',
  notes: '',
  requiresBatchTracking: false,
});

async function loadDropdowns(): Promise<void> {
  categoriesLoading.value = true;
  unitsLoading.value = true;
  try {
    const [cats, uoms] = await Promise.all([getAllCategories(), getAllUnits()]);
    categories.value = cats;
    units.value = uoms;
  } catch {
    // silent — dropdowns will be empty
  } finally {
    categoriesLoading.value = false;
    unitsLoading.value = false;
  }
}

onMounted(() => loadDropdowns());

async function populateForm(): Promise<void> {
  if (visible.value && props.product) {
    isEdit.value = true;
    try {
      const detail = await getProductById(props.product.id);
      form.name = detail.name;
      form.code = detail.code;
      form.sku = detail.sku ?? '';
      form.barcode = detail.barcode ?? '';
      form.categoryId = detail.categoryId;
      form.unitOfMeasureId = detail.unitOfMeasureId;
      form.description = detail.description ?? '';
      form.notes = detail.notes ?? '';
      form.requiresBatchTracking = detail.requiresBatchTracking;
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.name = '';
    form.code = '';
    form.sku = '';
    form.barcode = '';
    form.categoryId = null;
    form.unitOfMeasureId = null;
    form.description = '';
    form.notes = '';
    form.requiresBatchTracking = false;
  }
  fieldErrors.name = [];
  fieldErrors.code = [];
}

watch(visible, populateForm);
watch(() => props.product, populateForm);

const rules = {
  required: (v: string) => !!v || t('common.required'),
  requiredSelect: (v: number | null) => v !== null || t('common.required'),
  nameLength: (v: string) => !v || v.length <= 200 || t('validation.customerNameLength'),
  codeFormat: (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || t('validation.codeFormat'),
  codeLength: (v: string) => !v || v.length <= 50 || t('validation.codeLength'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    if (isEdit.value && props.product) {
      await updateProduct(props.product.id, {
        name: form.name,
        description: form.description || null,
        sku: form.sku || null,
        barcode: form.barcode || null,
        categoryId: form.categoryId,
        unitOfMeasureId: form.unitOfMeasureId!,
        notes: form.notes || null,
        requiresBatchTracking: form.requiresBatchTracking,
      });
      notification.success(t('products.edit') + ' \u2713');
    } else {
      await createProduct({
        name: form.name,
        code: form.code,
        description: form.description || null,
        sku: form.sku || null,
        barcode: form.barcode || null,
        categoryId: form.categoryId,
        unitOfMeasureId: form.unitOfMeasureId!,
        notes: form.notes || null,
        requiresBatchTracking: form.requiresBatchTracking,
      });
      notification.success(t('products.create') + ' \u2713');
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
  if (errorCode === 'DUPLICATE_PRODUCT_CODE') {
    fieldErrors.code = [err.response?.data?.detail || t('errors.DUPLICATE_PRODUCT_CODE')];
  } else if (errorCode === 'PRODUCT_CATEGORY_NOT_FOUND') {
    notification.error(t('errors.PRODUCT_CATEGORY_NOT_FOUND'));
  } else if (errorCode === 'UNIT_OF_MEASURE_NOT_FOUND') {
    notification.error(t('errors.UNIT_OF_MEASURE_NOT_FOUND'));
  } else if (errorCode) {
    const key = `errors.${errorCode}`;
    const translated = t(key);
    notification.error(translated !== key ? translated : t('errors.UNEXPECTED_ERROR'));
  } else {
    notification.error(getApiErrorMessage(err, t));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
