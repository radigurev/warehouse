<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="750" :title="t('adjustments.create')" icon="mdi-pencil-ruler" @back="cancel">
    <v-card-title class="text-h6">
      {{ t('adjustments.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.warehouseId"
              :label="t('adjustments.form.warehouse')"
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
            <v-text-field
              v-model="form.reason"
              :label="t('adjustments.form.reason')"
              prepend-inner-icon="mdi-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.required]"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('adjustments.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              rows="2"
              auto-grow
            />
          </v-col>
        </v-row>

        <v-divider class="my-4" />

        <div class="d-flex align-center mb-2">
          <span class="text-subtitle-2 font-weight-medium">{{ t('adjustments.detail.lines') }}</span>
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="addLine">
            {{ t('common.create') }}
          </v-btn>
        </div>

        <v-table :density="layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ t('adjustments.lines.product') }}</th>
              <th>{{ t('adjustments.lines.location') }}</th>
              <th>{{ t('adjustments.lines.actual') }}</th>
              <th style="width: 60px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="form.lines.length === 0">
              <td colspan="4" class="text-center text-medium-emphasis py-4">
                {{ t('adjustments.detail.noLines') }}
              </td>
            </tr>
            <tr v-for="(line, idx) in form.lines" :key="idx">
              <td>
                <v-autocomplete
                  v-model="line.productId"
                  :items="products"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  hide-details
                  :rules="[rules.required]"
                />
              </td>
              <td>
                <v-autocomplete
                  v-model="line.locationId"
                  :items="locations"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  hide-details
                  clearable
                />
              </td>
              <td>
                <v-text-field
                  v-model.number="line.actualQuantity"
                  type="number"
                  :density="layout.vuetifyDensity"
                  hide-details
                  :rules="[rules.required]"
                />
              </td>
              <td>
                <v-btn icon="mdi-delete" size="x-small" variant="text" color="error" @click="removeLine(idx)" />
              </td>
            </tr>
          </tbody>
        </v-table>
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
import { searchWarehouses } from '@features/inventory/api/warehouses';
import { searchLocations } from '@features/inventory/api/storage-locations';
import { createAdjustment } from '@features/inventory/api/adjustments';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import type {
  ProductDto,
  WarehouseDto,
  StorageLocationDto,
  CreateAdjustmentRequest,
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
  saved: [];
  cancelled: [];
}>();

interface AdjustmentLineForm {
  productId: number | null;
  locationId: number | null;
  actualQuantity: number | null;
}

const formRef = ref();
const submitting = ref(false);

const products = ref<ProductDto[]>([]);
const productsLoading = ref(false);
const warehouses = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const locations = ref<StorageLocationDto[]>([]);
const locationsLoading = ref(false);

const form = reactive({
  warehouseId: null as number | null,
  reason: '',
  notes: '',
  lines: [] as AdjustmentLineForm[],
});

const rules = {
  required: (v: unknown) => (v !== null && v !== undefined && v !== '') || t('common.required'),
};

async function loadDropdowns(): Promise<void> {
  productsLoading.value = true;
  warehousesLoading.value = true;
  locationsLoading.value = true;

  try {
    const [prodRes, whRes, locRes] = await Promise.all([
      searchProducts({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchWarehouses({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchLocations({ page: 1, pageSize: 1000 }),
    ]);
    products.value = prodRes.items;
    warehouses.value = whRes.items;
    locations.value = locRes.items;
  } catch {
    // dropdowns empty
  } finally {
    productsLoading.value = false;
    warehousesLoading.value = false;
    locationsLoading.value = false;
  }
}

onMounted(() => loadDropdowns());

watch(visible, (val) => {
  if (val) resetForm();
});

function resetForm(): void {
  form.warehouseId = null;
  form.reason = '';
  form.notes = '';
  form.lines = [];
}

function addLine(): void {
  form.lines.push({ productId: null, locationId: null, actualQuantity: null });
}

function removeLine(index: number): void {
  form.lines.splice(index, 1);
}

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  if (form.lines.length === 0) {
    notification.warning(t('adjustments.detail.noLines'));
    return;
  }

  submitting.value = true;
  try {
    const request: CreateAdjustmentRequest = {
      warehouseId: form.warehouseId!,
      reason: form.reason,
      notes: form.notes || undefined,
      lines: form.lines.map((line) => ({
        productId: line.productId!,
        locationId: line.locationId ?? undefined,
        actualQuantity: line.actualQuantity!,
      })),
    };
    await createAdjustment(request);
    notification.success(t('adjustments.create') + ' \u2713');
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
