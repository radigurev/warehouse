<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="500" :title="isEdit ? t('batches.edit') : t('batches.create')" :icon="isEdit ? 'mdi-barcode' : 'mdi-barcode'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('batches.edit') : t('batches.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-if="!isEdit" v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.productId"
              :label="t('batches.form.product')"
              prepend-inner-icon="mdi-package-variant-closed"
              :density="layout.vuetifyDensity"
              :items="products"
              item-title="name"
              item-value="id"
              :loading="productsLoading"
              :rules="[rules.required]"
            />
          </v-col>

          <v-col v-if="!isEdit" v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.batchNumber"
              :label="t('batches.form.batchNumber')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
              :rules="[rules.required]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.expiryDate"
              :label="t('batches.form.expiryDate')"
              prepend-inner-icon="mdi-calendar"
              :density="layout.vuetifyDensity"
              type="date"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('batches.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
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
      <v-btn color="primary" variant="flat" :loading="submitting" @click="handleSubmit">
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
import { searchProducts } from '@features/inventory/api/products';
import { getBatchById, createBatch, updateBatch } from '@features/inventory/api/batches';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import type { ProductDto, BatchDto } from '@features/inventory/types/inventory';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  batch?: BatchDto | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const submitting = ref(false);

const products = ref<ProductDto[]>([]);
const productsLoading = ref(false);

const form = reactive({
  productId: null as number | null,
  batchNumber: '',
  expiryDate: '',
  notes: '',
});

const rules = {
  required: (v: unknown) => (v !== null && v !== undefined && v !== '') || t('common.required'),
};

async function loadProducts(): Promise<void> {
  productsLoading.value = true;
  try {
    const res = await searchProducts({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 });
    products.value = res.items;
  } catch {
    // dropdown empty
  } finally {
    productsLoading.value = false;
  }
}

onMounted(() => loadProducts());

async function populateForm(): Promise<void> {
  if (visible.value && props.batch) {
    isEdit.value = true;
    try {
      const detail = await getBatchById(props.batch.id);
      form.productId = detail.productId;
      form.batchNumber = detail.batchNumber;
      form.expiryDate = detail.expiryDate ? detail.expiryDate.substring(0, 10) : '';
      form.notes = detail.notes ?? '';
    } catch {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.productId = null;
    form.batchNumber = '';
    form.expiryDate = '';
    form.notes = '';
  }
}

watch(visible, populateForm);
watch(() => props.batch, populateForm);

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  submitting.value = true;
  try {
    if (isEdit.value && props.batch) {
      await updateBatch(props.batch.id, {
        expiryDate: form.expiryDate || null,
        notes: form.notes || null,
      });
      notification.success(t('batches.edit') + ' \u2713');
    } else {
      await createBatch({
        productId: form.productId!,
        batchNumber: form.batchNumber,
        expiryDate: form.expiryDate || null,
        notes: form.notes || null,
      });
      notification.success(t('batches.create') + ' \u2713');
    }
    visible.value = false;
    emit('saved');
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    submitting.value = false;
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
