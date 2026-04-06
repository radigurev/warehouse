<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="600" :title="t('stockMovements.record')" icon="mdi-swap-horizontal" @back="cancel">
    <v-card-title class="text-h6">
      {{ t('stockMovements.record') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.productId"
              :label="t('stockMovements.form.product')"
              prepend-inner-icon="mdi-package-variant-closed"
              :density="layout.vuetifyDensity"
              :items="products"
              item-title="name"
              item-value="id"
              :loading="productsLoading"
              :rules="[rules.required]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.warehouseId"
              :label="t('stockMovements.form.warehouse')"
              prepend-inner-icon="mdi-warehouse"
              :density="layout.vuetifyDensity"
              :items="warehouses"
              item-title="name"
              item-value="id"
              :loading="warehousesLoading"
              :rules="[rules.required]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.locationId"
              :label="t('stockMovements.form.location')"
              prepend-inner-icon="mdi-map-marker"
              :density="layout.vuetifyDensity"
              :items="locations"
              item-title="name"
              item-value="id"
              :loading="locationsLoading"
              clearable
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model.number="form.quantity"
              :label="t('stockMovements.form.quantity')"
              prepend-inner-icon="mdi-numeric"
              :density="layout.vuetifyDensity"
              type="number"
              :rules="[rules.required, rules.positiveNumber]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.reasonCode"
              :label="t('stockMovements.form.reasonCode')"
              prepend-inner-icon="mdi-tag"
              :density="layout.vuetifyDensity"
              :items="reasonOptions"
              :rules="[rules.required]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.batchId"
              :label="t('stockMovements.form.batch')"
              prepend-inner-icon="mdi-barcode"
              :density="layout.vuetifyDensity"
              :items="batches"
              item-title="batchNumber"
              item-value="id"
              :loading="batchesLoading"
              clearable
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.referenceNumber"
              :label="t('stockMovements.form.referenceNumber')"
              prepend-inner-icon="mdi-identifier"
              :density="layout.vuetifyDensity"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('stockMovements.form.notes')"
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
import { ref, reactive, watch, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { searchProducts } from '@features/inventory/api/products';
import { searchWarehouses } from '@features/inventory/api/warehouses';
import { searchLocations } from '@features/inventory/api/storage-locations';
import { searchBatches } from '@features/inventory/api/batches';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import type {
  ProductDto,
  WarehouseDto,
  StorageLocationDto,
  BatchDto,
  RecordStockMovementRequest,
  StockMovementReason,
} from '@features/inventory/types/inventory';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

defineProps<{
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [request: RecordStockMovementRequest];
  cancelled: [];
}>();

const formRef = ref();
const submitting = ref(false);

const products = ref<ProductDto[]>([]);
const productsLoading = ref(false);
const warehouses = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const locations = ref<StorageLocationDto[]>([]);
const locationsLoading = ref(false);
const batches = ref<BatchDto[]>([]);
const batchesLoading = ref(false);

const reasonCodes: StockMovementReason[] = [
  'PurchaseReceipt', 'SalesDispatch', 'Adjustment', 'Transfer',
  'CustomerReturn', 'SupplierReturn', 'ProductionConsumption',
  'ProductionReceipt', 'WriteOff', 'StocktakeCorrection', 'Other',
];

const reasonOptions = computed(() =>
  reasonCodes.map((code) => ({
    title: t(`stockMovements.reasons.${code}`),
    value: code,
  })),
);

const form = reactive({
  productId: null as number | null,
  warehouseId: null as number | null,
  locationId: null as number | null,
  quantity: null as number | null,
  reasonCode: null as string | null,
  batchId: null as number | null,
  referenceNumber: '',
  notes: '',
});

const rules = {
  required: (v: unknown) => (v !== null && v !== undefined && v !== '') || t('common.required'),
  positiveNumber: (v: number | null) => (v !== null && v > 0) || t('common.required'),
};

async function loadDropdowns(): Promise<void> {
  productsLoading.value = true;
  warehousesLoading.value = true;
  locationsLoading.value = true;
  batchesLoading.value = true;

  try {
    const [prodRes, whRes, locRes, batchRes] = await Promise.all([
      searchProducts({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchWarehouses({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchLocations({ page: 1, pageSize: 1000 }),
      searchBatches({ includeExpired: false, page: 1, pageSize: 1000 }),
    ]);
    products.value = prodRes.items;
    warehouses.value = whRes.items;
    locations.value = locRes.items;
    batches.value = batchRes.items;
  } catch {
    // dropdowns will be empty
  } finally {
    productsLoading.value = false;
    warehousesLoading.value = false;
    locationsLoading.value = false;
    batchesLoading.value = false;
  }
}

onMounted(() => loadDropdowns());

watch(visible, (val) => {
  if (val) resetForm();
});

function resetForm(): void {
  form.productId = null;
  form.warehouseId = null;
  form.locationId = null;
  form.quantity = null;
  form.reasonCode = null;
  form.batchId = null;
  form.referenceNumber = '';
  form.notes = '';
}

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  submitting.value = true;
  try {
    const request: RecordStockMovementRequest = {
      productId: form.productId!,
      warehouseId: form.warehouseId!,
      locationId: form.locationId ?? undefined,
      quantity: form.quantity!,
      reasonCode: form.reasonCode!,
      batchId: form.batchId ?? undefined,
      referenceNumber: form.referenceNumber || undefined,
      notes: form.notes || undefined,
    };
    emit('saved', request);
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
