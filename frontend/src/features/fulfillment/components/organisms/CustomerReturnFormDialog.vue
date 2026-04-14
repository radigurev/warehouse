<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="900" :title="t('customerReturns.create')" icon="mdi-package-variant-minus" @back="cancel">
    <v-card-title class="text-h6">
      {{ t('customerReturns.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.customerId"
              :label="t('customerReturns.form.customer')"
              prepend-inner-icon="mdi-account"
              :density="layout.vuetifyDensity"
              :items="customers"
              item-title="name"
              item-value="id"
              :loading="customersLoading"
              :rules="[rules.requiredSelect]"
              @update:model-value="onCustomerChanged"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.salesOrderId"
              :label="t('customerReturns.form.salesOrder')"
              prepend-inner-icon="mdi-file-document"
              :density="layout.vuetifyDensity"
              :items="customerSalesOrders"
              item-title="orderNumber"
              item-value="id"
              :loading="ordersLoading"
              clearable
              :disabled="!form.customerId"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.reason"
              :label="t('customerReturns.form.reason')"
              prepend-inner-icon="mdi-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.reasonLength]"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('customerReturns.form.notes')"
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
          <div class="text-subtitle-1 font-weight-medium">{{ t('customerReturns.detail.lines') }}</div>
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="addLine">
            {{ t('common.create') }}
          </v-btn>
        </div>

        <v-table :density="layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ t('customerReturns.lines.product') }}</th>
              <th>{{ t('customerReturns.lines.warehouse') }}</th>
              <th>{{ t('customerReturns.lines.location') }}</th>
              <th>{{ t('customerReturns.lines.quantity') }}</th>
              <th>{{ t('customerReturns.lines.notes') }}</th>
              <th style="width: 60px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="form.lines.length === 0">
              <td colspan="6" class="text-center text-medium-emphasis py-4">
                {{ t('customerReturns.detail.noLines') }}
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
              <td style="min-width: 160px">
                <v-select
                  v-model="line.warehouseId"
                  :items="warehouses"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  :loading="warehousesLoading"
                  @update:model-value="onLineWarehouseChanged(idx)"
                />
              </td>
              <td style="min-width: 160px">
                <v-select
                  v-model="line.locationId"
                  :items="lineLocationOptions[idx] || []"
                  item-title="name"
                  item-value="id"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  clearable
                />
              </td>
              <td style="min-width: 100px">
                <v-text-field
                  v-model.number="line.quantity"
                  type="number"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  :min="0.01"
                  step="any"
                />
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
import { ref, reactive, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { createCustomerReturn } from '@features/fulfillment/api/customer-returns';
import { searchSalesOrders } from '@features/fulfillment/api/sales-orders';
import { searchCustomers } from '@features/customers/api/customers';
import { searchProducts } from '@features/inventory/api/products';
import { searchWarehouses } from '@features/inventory/api/warehouses';
import { searchLocations } from '@features/inventory/api/storage-locations';
import type { CustomerDto } from '@features/customers/types/customer';
import type { ProductDto, WarehouseDto, StorageLocationDto } from '@features/inventory/types/inventory';
import type { SalesOrderDto } from '@features/fulfillment/types/fulfillment';

interface ReturnLineForm {
  productId: number | null;
  warehouseId: number | null;
  locationId: number | null;
  quantity: number;
  notes: string;
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
}>();

const formRef = ref();
const loading = ref(false);
const customers = ref<CustomerDto[]>([]);
const customersLoading = ref(false);
const products = ref<ProductDto[]>([]);
const productsLoading = ref(false);
const warehouses = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const customerSalesOrders = ref<SalesOrderDto[]>([]);
const ordersLoading = ref(false);
const lineLocationOptions = ref<Record<number, StorageLocationDto[]>>({});

const form = reactive({
  customerId: null as number | null,
  salesOrderId: null as number | null,
  reason: '',
  notes: '',
  lines: [] as ReturnLineForm[],
});

async function loadDropdowns(): Promise<void> {
  customersLoading.value = true;
  productsLoading.value = true;
  warehousesLoading.value = true;
  try {
    const [custRes, prodRes, whRes] = await Promise.all([
      searchCustomers({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchProducts({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchWarehouses({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
    ]);
    customers.value = custRes.items;
    products.value = prodRes.items;
    warehouses.value = whRes.items;
  } catch {
    // silent
  } finally {
    customersLoading.value = false;
    productsLoading.value = false;
    warehousesLoading.value = false;
  }
}

onMounted(() => loadDropdowns());

watch(visible, (val) => {
  if (val) {
    form.customerId = null;
    form.salesOrderId = null;
    form.reason = '';
    form.notes = '';
    form.lines = [];
    customerSalesOrders.value = [];
    lineLocationOptions.value = {};
  }
});

async function onCustomerChanged(): Promise<void> {
  form.salesOrderId = null;
  customerSalesOrders.value = [];
  if (!form.customerId) return;
  ordersLoading.value = true;
  try {
    const result = await searchSalesOrders({
      customerId: form.customerId,
      sortDescending: false,
      page: 1,
      pageSize: 1000,
    });
    customerSalesOrders.value = result.items;
  } catch {
    customerSalesOrders.value = [];
  } finally {
    ordersLoading.value = false;
  }
}

function addLine(): void {
  form.lines.push({ productId: null, warehouseId: null, locationId: null, quantity: 1, notes: '' });
}

function removeLine(index: number): void {
  form.lines.splice(index, 1);
  delete lineLocationOptions.value[index];
}

async function onLineWarehouseChanged(index: number): Promise<void> {
  const line = form.lines[index];
  line.locationId = null;
  lineLocationOptions.value[index] = [];

  if (!line.warehouseId) return;

  try {
    const response = await searchLocations({ warehouseId: line.warehouseId, page: 1, pageSize: 1000 });
    lineLocationOptions.value[index] = response.items;
  } catch {
    lineLocationOptions.value[index] = [];
  }
}

const rules = {
  required: (v: string) => !!v || t('common.required'),
  requiredSelect: (v: number | null) => v !== null || t('common.required'),
  reasonLength: (v: string) => !v || v.length <= 500 || t('validation.reasonLength'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  if (form.lines.length === 0) {
    notification.error(t('customerReturns.detail.noLines'));
    return;
  }

  const invalidLines = form.lines.some(
    (l) => !l.productId || !l.warehouseId || !l.quantity || l.quantity <= 0,
  );
  if (invalidLines) {
    notification.error(t('errors.VALIDATION_ERROR'));
    return;
  }

  loading.value = true;
  try {
    await createCustomerReturn({
      customerId: form.customerId!,
      salesOrderId: form.salesOrderId,
      reason: form.reason,
      notes: form.notes || null,
      lines: form.lines.map((l) => ({
        productId: l.productId!,
        warehouseId: l.warehouseId!,
        locationId: l.locationId ?? undefined,
        quantity: l.quantity,
        notes: l.notes || null,
      })),
    });
    notification.success(t('customerReturns.create') + ' \u2713');
    visible.value = false;
    emit('saved');
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
