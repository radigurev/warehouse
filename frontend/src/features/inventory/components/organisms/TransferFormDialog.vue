<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="800" :title="t('transfers.create')" icon="mdi-truck" @back="cancel">
    <v-card-title class="text-h6">
      {{ t('transfers.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.sourceWarehouseId"
              :label="t('transfers.form.sourceWarehouse')"
              prepend-inner-icon="mdi-warehouse"
              :density="layout.vuetifyDensity"
              :items="warehouseOptions"
              item-title="name"
              item-value="id"
              :loading="warehousesLoading"
              :rules="[rules.required]"
              :error-messages="fieldErrors.sourceWarehouseId"
              @update:model-value="onSourceChanged"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.destinationWarehouseId"
              :label="t('transfers.form.destinationWarehouse')"
              prepend-inner-icon="mdi-warehouse"
              :density="layout.vuetifyDensity"
              :items="warehouseOptions"
              item-title="name"
              item-value="id"
              :loading="warehousesLoading"
              :rules="[rules.required, rules.differentWarehouse]"
              :error-messages="fieldErrors.destinationWarehouseId"
              @update:model-value="onDestinationChanged"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('transfers.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.notesLength]"
              rows="2"
              auto-grow
            />
          </v-col>
        </v-row>

        <v-divider class="my-4" />

        <div class="d-flex align-center mb-2">
          <div class="text-subtitle-1 font-weight-medium">{{ t('transfers.detail.lines') }}</div>
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="addLine">
            {{ t('common.create') }}
          </v-btn>
        </div>

        <v-table :density="layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ t('transfers.lines.product') }}</th>
              <th>{{ t('transfers.lines.quantity') }}</th>
              <th>{{ t('transfers.lines.sourceLocation') }}</th>
              <th>{{ t('transfers.lines.destinationLocation') }}</th>
              <th style="width: 60px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(line, idx) in form.lines" :key="idx">
              <td style="min-width: 180px">
                <v-select
                  v-model="line.productId"
                  :items="productOptions"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  :loading="productsLoading"
                  hide-details
                  variant="underlined"
                />
              </td>
              <td style="min-width: 100px">
                <v-text-field
                  v-model.number="line.quantity"
                  type="number"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  :min="1"
                />
              </td>
              <td style="min-width: 160px">
                <v-select
                  v-model="line.sourceLocationId"
                  :items="sourceLocationOptions"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  clearable
                />
              </td>
              <td style="min-width: 160px">
                <v-select
                  v-model="line.destinationLocationId"
                  :items="destinationLocationOptions"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  clearable
                />
              </td>
              <td>
                <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="removeLine(idx)" />
              </td>
            </tr>
            <tr v-if="form.lines.length === 0">
              <td colspan="5" class="text-center text-medium-emphasis py-4">
                {{ t('transfers.detail.noLines') }}
              </td>
            </tr>
          </tbody>
        </v-table>
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
import { searchWarehouses } from '@features/inventory/api/warehouses';
import { searchLocations } from '@features/inventory/api/storage-locations';
import { searchProducts } from '@features/inventory/api/products';
import { createTransfer } from '@features/inventory/api/transfers';
import type { WarehouseDto, ProductDto, StorageLocationDto } from '@features/inventory/types/inventory';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

interface TransferLineForm {
  productId: number | null;
  quantity: number;
  sourceLocationId: number | null;
  destinationLocationId: number | null;
}

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

defineProps<{
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
  back: [];
}>();

const formRef = ref();
const loading = ref(false);
const warehouseOptions = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const productOptions = ref<ProductDto[]>([]);
const productsLoading = ref(false);
const sourceLocationOptions = ref<StorageLocationDto[]>([]);
const destinationLocationOptions = ref<StorageLocationDto[]>([]);

const fieldErrors = reactive<Record<string, string[]>>({
  sourceWarehouseId: [],
  destinationWarehouseId: [],
});

const form = reactive({
  sourceWarehouseId: null as number | null,
  destinationWarehouseId: null as number | null,
  notes: '',
  lines: [] as TransferLineForm[],
});

onMounted(() => {
  loadWarehouses();
  loadProducts();
});

watch(visible, () => {
  if (visible.value) {
    form.sourceWarehouseId = null;
    form.destinationWarehouseId = null;
    form.notes = '';
    form.lines = [];
    sourceLocationOptions.value = [];
    destinationLocationOptions.value = [];
    fieldErrors.sourceWarehouseId = [];
    fieldErrors.destinationWarehouseId = [];
  }
});

async function loadWarehouses(): Promise<void> {
  warehousesLoading.value = true;
  try {
    const response = await searchWarehouses({ includeDeleted: false, page: 1, pageSize: 500, sortDescending: false });
    warehouseOptions.value = response.items;
  } catch {
    // silent
  } finally {
    warehousesLoading.value = false;
  }
}

async function loadProducts(): Promise<void> {
  productsLoading.value = true;
  try {
    const response = await searchProducts({ includeDeleted: false, page: 1, pageSize: 500, sortDescending: false });
    productOptions.value = response.items;
  } catch {
    // silent
  } finally {
    productsLoading.value = false;
  }
}

async function onSourceChanged(): Promise<void> {
  fieldErrors.sourceWarehouseId = [];
  if (form.sourceWarehouseId) {
    try {
      const response = await searchLocations({ warehouseId: form.sourceWarehouseId, page: 1, pageSize: 500 });
      sourceLocationOptions.value = response.items;
    } catch {
      sourceLocationOptions.value = [];
    }
  } else {
    sourceLocationOptions.value = [];
  }
}

async function onDestinationChanged(): Promise<void> {
  fieldErrors.destinationWarehouseId = [];
  if (form.destinationWarehouseId) {
    try {
      const response = await searchLocations({ warehouseId: form.destinationWarehouseId, page: 1, pageSize: 500 });
      destinationLocationOptions.value = response.items;
    } catch {
      destinationLocationOptions.value = [];
    }
  } else {
    destinationLocationOptions.value = [];
  }
}

function addLine(): void {
  form.lines.push({ productId: null, quantity: 1, sourceLocationId: null, destinationLocationId: null });
}

function removeLine(index: number): void {
  form.lines.splice(index, 1);
}

const rules = {
  required: (v: unknown) => !!v || t('common.required'),
  differentWarehouse: (v: number | null) => v !== form.sourceWarehouseId || t('errors.TRANSFER_SAME_WAREHOUSE'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  if (form.lines.length === 0) {
    notification.error(t('transfers.detail.noLines'));
    return;
  }

  const invalidLines = form.lines.some((l) => !l.productId || l.quantity <= 0);
  if (invalidLines) {
    notification.error(t('errors.VALIDATION_ERROR'));
    return;
  }

  loading.value = true;
  try {
    await createTransfer({
      sourceWarehouseId: form.sourceWarehouseId!,
      destinationWarehouseId: form.destinationWarehouseId!,
      notes: form.notes || null,
      lines: form.lines.map((l) => ({
        productId: l.productId!,
        quantity: l.quantity,
        sourceLocationId: l.sourceLocationId ?? undefined,
        destinationLocationId: l.destinationLocationId ?? undefined,
      })),
    });
    notification.success(t('transfers.create'));
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
  if (errorCode === 'TRANSFER_SAME_WAREHOUSE') {
    fieldErrors.destinationWarehouseId = [t('errors.TRANSFER_SAME_WAREHOUSE')];
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
