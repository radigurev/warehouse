<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="900" :title="isEdit ? t('purchaseOrders.edit') : t('purchaseOrders.create')" :icon="isEdit ? 'mdi-file-document-edit' : 'mdi-file-document-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('purchaseOrders.edit') : t('purchaseOrders.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.supplierId"
              :label="t('purchaseOrders.form.supplier')"
              prepend-inner-icon="mdi-truck-delivery"
              :density="layout.vuetifyDensity"
              :items="suppliers"
              item-title="name"
              item-value="id"
              :loading="suppliersLoading"
              :rules="[rules.requiredSelect]"
              :readonly="isEdit"
              :error-messages="fieldErrors.supplierId"
              @update:model-value="fieldErrors.supplierId = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.destinationWarehouseId"
              :label="t('purchaseOrders.form.warehouse')"
              prepend-inner-icon="mdi-warehouse"
              :density="layout.vuetifyDensity"
              :items="warehouses"
              item-title="name"
              item-value="id"
              :loading="warehousesLoading"
              :rules="[rules.requiredSelect]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-menu v-model="deliveryDateMenu" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-bind="menuProps"
                  :model-value="form.expectedDeliveryDate"
                  :label="t('purchaseOrders.form.expectedDeliveryDate')"
                  prepend-inner-icon="mdi-calendar"
                  :density="layout.vuetifyDensity"
                  readonly
                  clearable
                  @click:clear="form.expectedDeliveryDate = ''"
                />
              </template>
              <v-date-picker
                :model-value="form.expectedDeliveryDate ? new Date(form.expectedDeliveryDate + 'T00:00:00') : undefined"
                color="primary"
                show-adjacent-months
                @update:model-value="onDeliveryDatePicked"
              />
            </v-menu>
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('purchaseOrders.form.notes')"
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
          <div class="text-subtitle-1 font-weight-medium">{{ t('purchaseOrders.detail.lines') }}</div>
          <v-spacer />
          <div v-if="form.lines.length > 0" class="text-subtitle-2 font-weight-medium mr-4">
            {{ t('purchaseOrders.form.grandTotal') }}: {{ grandTotal.toFixed(2) }}
          </div>
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="addLine">
            {{ t('common.create') }}
          </v-btn>
        </div>

        <v-table :density="layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ t('purchaseOrders.lines.product') }}</th>
              <th>{{ t('purchaseOrders.lines.orderedQuantity') }}</th>
              <th>{{ t('purchaseOrders.lines.unitPrice') }}</th>
              <th>{{ t('purchaseOrders.lines.lineTotal') }}</th>
              <th>{{ t('purchaseOrders.lines.notes') }}</th>
              <th style="width: 60px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="form.lines.length === 0">
              <td colspan="6" class="text-center text-medium-emphasis py-4">
                {{ t('purchaseOrders.detail.noLines') }}
              </td>
            </tr>
            <tr v-for="(line, idx) in form.lines" :key="idx">
              <td style="min-width: 200px">
                <v-autocomplete
                  v-model="line.productId"
                  :items="products"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  :loading="productsLoading"
                />
              </td>
              <td style="min-width: 100px">
                <v-text-field
                  v-model.number="line.orderedQuantity"
                  type="number"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  :min="0.01"
                  step="any"
                />
              </td>
              <td style="min-width: 100px">
                <v-text-field
                  v-model.number="line.unitPrice"
                  type="number"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  :min="0"
                  step="any"
                />
              </td>
              <td class="text-right" style="min-width: 80px">
                {{ ((line.orderedQuantity || 0) * (line.unitPrice || 0)).toFixed(2) }}
              </td>
              <td style="min-width: 120px">
                <v-text-field
                  v-model="line.notes"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                />
              </td>
              <td>
                <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="removeLine(idx)" />
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
import { ref, reactive, computed, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import {
  createPurchaseOrder,
  updatePurchaseOrder,
  getPurchaseOrderById,
  addPurchaseOrderLine,
  updatePurchaseOrderLine,
  deletePurchaseOrderLine,
} from '@features/purchasing/api/purchase-orders';
import { searchSuppliers } from '@features/purchasing/api/suppliers';
import { searchProducts } from '@features/inventory/api/products';
import { searchWarehouses } from '@features/inventory/api/warehouses';
import type {
  SupplierDto,
  PurchaseOrderDetailDto,
  PurchaseOrderLineDto,
} from '@features/purchasing/types/purchasing';
import type { ProductDto, WarehouseDto } from '@features/inventory/types/inventory';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

interface OrderLineForm {
  id: number | null;
  productId: number | null;
  orderedQuantity: number;
  unitPrice: number;
  notes: string;
}

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  poId?: number | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const suppliers = ref<SupplierDto[]>([]);
const suppliersLoading = ref(false);
const warehouses = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const products = ref<ProductDto[]>([]);
const productsLoading = ref(false);
const originalLines = ref<PurchaseOrderLineDto[]>([]);

const deliveryDateMenu = ref(false);

const fieldErrors = reactive<Record<string, string[]>>({
  supplierId: [],
});

const form = reactive({
  supplierId: null as number | null,
  destinationWarehouseId: null as number | null,
  expectedDeliveryDate: '',
  notes: '',
  lines: [] as OrderLineForm[],
});

const grandTotal = computed(() => {
  return form.lines.reduce((sum, line) => sum + (line.orderedQuantity || 0) * (line.unitPrice || 0), 0);
});

async function loadDropdowns(): Promise<void> {
  suppliersLoading.value = true;
  warehousesLoading.value = true;
  productsLoading.value = true;
  try {
    const [suppRes, whRes, prodRes] = await Promise.all([
      searchSuppliers({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchWarehouses({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchProducts({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
    ]);
    suppliers.value = suppRes.items;
    warehouses.value = whRes.items;
    products.value = prodRes.items;
  } catch {
    // silent
  } finally {
    suppliersLoading.value = false;
    warehousesLoading.value = false;
    productsLoading.value = false;
  }
}

onMounted(() => loadDropdowns());

async function populateForm(): Promise<void> {
  if (visible.value && props.poId) {
    isEdit.value = true;
    try {
      const detail: PurchaseOrderDetailDto = await getPurchaseOrderById(props.poId);
      form.supplierId = detail.supplierId;
      form.destinationWarehouseId = detail.destinationWarehouseId;
      form.expectedDeliveryDate = detail.expectedDeliveryDate ? detail.expectedDeliveryDate.substring(0, 10) : '';
      form.notes = detail.notes ?? '';
      originalLines.value = [...detail.lines];
      form.lines = detail.lines.map((l) => ({
        id: l.id,
        productId: l.productId,
        orderedQuantity: l.orderedQuantity,
        unitPrice: l.unitPrice,
        notes: l.notes ?? '',
      }));
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.supplierId = null;
    form.destinationWarehouseId = null;
    form.expectedDeliveryDate = '';
    form.notes = '';
    form.lines = [];
    originalLines.value = [];
  }
  fieldErrors.supplierId = [];
}

watch(visible, populateForm);
watch(() => props.poId, populateForm);

function addLine(): void {
  form.lines.push({ id: null, productId: null, orderedQuantity: 1, unitPrice: 0, notes: '' });
}

function removeLine(index: number): void {
  form.lines.splice(index, 1);
}

function onDeliveryDatePicked(value: unknown): void {
  if (value instanceof Date) {
    form.expectedDeliveryDate = value.toISOString().split('T')[0];
  } else {
    form.expectedDeliveryDate = '';
  }
  deliveryDateMenu.value = false;
}

const rules = {
  requiredSelect: (v: number | null) => v !== null || t('common.required'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  if (form.lines.length === 0) {
    notification.error(t('purchaseOrders.detail.noLines'));
    return;
  }

  const invalidLines = form.lines.some((l) => !l.productId || !l.orderedQuantity || l.orderedQuantity <= 0 || l.unitPrice < 0);
  if (invalidLines) {
    notification.error(t('errors.VALIDATION_ERROR'));
    return;
  }

  loading.value = true;
  try {
    if (isEdit.value && props.poId) {
      await updatePurchaseOrder(props.poId, {
        destinationWarehouseId: form.destinationWarehouseId!,
        expectedDeliveryDate: form.expectedDeliveryDate || null,
        notes: form.notes || null,
      });

      const originalIds = new Set(originalLines.value.map((l) => l.id));
      const currentIds = new Set(form.lines.filter((l) => l.id !== null).map((l) => l.id!));

      const deletedLineIds = [...originalIds].filter((id) => !currentIds.has(id));
      for (const lineId of deletedLineIds) {
        await deletePurchaseOrderLine(props.poId, lineId);
      }

      for (const line of form.lines) {
        if (line.id !== null) {
          await updatePurchaseOrderLine(props.poId, line.id, {
            orderedQuantity: line.orderedQuantity,
            unitPrice: line.unitPrice,
            notes: line.notes || null,
          });
        } else {
          await addPurchaseOrderLine(props.poId, {
            productId: line.productId!,
            orderedQuantity: line.orderedQuantity,
            unitPrice: line.unitPrice,
            notes: line.notes || null,
          });
        }
      }

      notification.success(t('purchaseOrders.edit') + ' \u2713');
    } else {
      await createPurchaseOrder({
        supplierId: form.supplierId!,
        destinationWarehouseId: form.destinationWarehouseId!,
        expectedDeliveryDate: form.expectedDeliveryDate || null,
        notes: form.notes || null,
        lines: form.lines.map((l) => ({
          productId: l.productId!,
          orderedQuantity: l.orderedQuantity,
          unitPrice: l.unitPrice,
          notes: l.notes || null,
        })),
      });
      notification.success(t('purchaseOrders.create') + ' \u2713');
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
  if (errorCode === 'SUPPLIER_NOT_FOUND') {
    fieldErrors.supplierId = [err.response?.data?.detail || t('errors.SUPPLIER_NOT_FOUND')];
  } else {
    notification.error(getApiErrorMessage(err, t));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
