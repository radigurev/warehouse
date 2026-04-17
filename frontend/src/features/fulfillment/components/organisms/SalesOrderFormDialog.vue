<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="900" :title="isEdit ? t('salesOrders.edit') : t('salesOrders.create')" :icon="isEdit ? 'mdi-file-document-edit' : 'mdi-file-document-plus'" @back="cancel">
    <v-card-title class="text-h6">
      {{ isEdit ? t('salesOrders.edit') : t('salesOrders.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.customerId"
              :label="t('salesOrders.form.customer')"
              prepend-inner-icon="mdi-account"
              :density="layout.vuetifyDensity"
              :items="customers"
              item-title="name"
              item-value="id"
              :loading="customersLoading"
              :rules="[rules.requiredSelect]"
              :readonly="isEdit"
              :error-messages="fieldErrors.customerId"
              @update:model-value="fieldErrors.customerId = []"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.warehouseId"
              :label="t('salesOrders.form.warehouse')"
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
            <v-menu v-model="shipDateMenu" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  v-bind="menuProps"
                  :model-value="form.requestedShipDate"
                  :label="t('salesOrders.form.requestedShipDate')"
                  prepend-inner-icon="mdi-calendar"
                  :density="layout.vuetifyDensity"
                  readonly
                  clearable
                  @click:clear="form.requestedShipDate = ''"
                />
              </template>
              <v-date-picker
                :model-value="form.requestedShipDate ? new Date(form.requestedShipDate + 'T00:00:00') : undefined"
                color="primary"
                show-adjacent-months
                @update:model-value="onShipDatePicked"
              />
            </v-menu>
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.carrierId"
              :label="t('salesOrders.form.carrier')"
              prepend-inner-icon="mdi-truck-delivery"
              :density="layout.vuetifyDensity"
              :items="carriers"
              item-title="name"
              item-value="id"
              :loading="carriersLoading"
              clearable
              @update:model-value="onCarrierChanged"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.carrierServiceLevelId"
              :label="t('salesOrders.form.serviceLevel')"
              prepend-inner-icon="mdi-speedometer"
              :density="layout.vuetifyDensity"
              :items="filteredServiceLevels"
              item-title="name"
              item-value="id"
              clearable
              :disabled="!form.carrierId"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('salesOrders.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.notesLength]"
              rows="2"
              auto-grow
            />
          </v-col>
        </v-row>

        <!-- Shipping Address Section -->
        <v-divider class="my-4" />
        <div class="text-subtitle-1 font-weight-medium mb-3">{{ t('salesOrders.form.shippingAddress') }}</div>
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.shippingStreetLine1"
              :label="t('salesOrders.form.streetLine1')"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.streetLength]"
            />
          </v-col>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.shippingStreetLine2"
              :label="t('salesOrders.form.streetLine2')"
              :density="layout.vuetifyDensity"
              :rules="[rules.streetLength]"
            />
          </v-col>
          <v-col v-bind="grid.fullCols">
            <NomenclatureAddressFields
              v-model:country-code="form.shippingCountryCode"
              v-model:state-province="form.shippingStateProvince"
              v-model:city="form.shippingCity"
              :density="layout.vuetifyDensity"
            />
          </v-col>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.shippingPostalCode"
              :label="t('salesOrders.form.postalCode')"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.postalCodeLength]"
            />
          </v-col>
        </v-row>

        <!-- Lines Section (Create mode only - edit uses separate line ops) -->
        <template v-if="!isEdit">
          <v-divider class="my-4" />
          <div class="d-flex align-center mb-2">
            <div class="text-subtitle-1 font-weight-medium">{{ t('salesOrders.detail.lines') }}</div>
            <v-spacer />
            <div v-if="form.lines.length > 0" class="text-subtitle-2 font-weight-medium mr-4">
              {{ t('salesOrders.form.grandTotal') }}: {{ grandTotal.toFixed(2) }}
            </div>
            <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="addLine">
              {{ t('common.create') }}
            </v-btn>
          </div>

          <v-table :density="layout.vuetifyDensity">
            <thead>
              <tr>
                <th>{{ t('salesOrders.lines.product') }}</th>
                <th>{{ t('salesOrders.lines.orderedQty') }}</th>
                <th>{{ t('salesOrders.lines.unitPrice') }}</th>
                <th>{{ t('salesOrders.lines.lineTotal') }}</th>
                <th>{{ t('salesOrders.lines.notes') }}</th>
                <th style="width: 60px"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="form.lines.length === 0">
                <td colspan="6" class="text-center text-medium-emphasis py-4">
                  {{ t('salesOrders.detail.noLines') }}
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
        </template>
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
import NomenclatureAddressFields from '@shared/components/molecules/NomenclatureAddressFields.vue';
import {
  createSalesOrder,
  updateSalesOrder,
  getSalesOrderById,
} from '@features/fulfillment/api/sales-orders';
import { searchCarriers, getCarrierById } from '@features/fulfillment/api/carriers';
import { searchCustomers } from '@features/customers/api/customers';
import { searchProducts } from '@features/inventory/api/products';
import { searchWarehouses } from '@features/inventory/api/warehouses';
import type { CustomerDto } from '@features/customers/types/customer';
import type { ProductDto, WarehouseDto } from '@features/inventory/types/inventory';
import type { CarrierDto, CarrierServiceLevelDto, SalesOrderDetailDto } from '@features/fulfillment/types/fulfillment';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

interface OrderLineForm {
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
  soId?: number | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const isEdit = ref(false);
const formRef = ref();
const loading = ref(false);
const customers = ref<CustomerDto[]>([]);
const customersLoading = ref(false);
const warehouses = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const products = ref<ProductDto[]>([]);
const productsLoading = ref(false);
const carriers = ref<CarrierDto[]>([]);
const carriersLoading = ref(false);
const allServiceLevels = ref<CarrierServiceLevelDto[]>([]);

const shipDateMenu = ref(false);

const fieldErrors = reactive<Record<string, string[]>>({
  customerId: [],
});

const form = reactive({
  customerId: null as number | null,
  warehouseId: null as number | null,
  requestedShipDate: '',
  carrierId: null as number | null,
  carrierServiceLevelId: null as number | null,
  shippingStreetLine1: '',
  shippingStreetLine2: '',
  shippingCity: '',
  shippingStateProvince: '',
  shippingPostalCode: '',
  shippingCountryCode: '',
  notes: '',
  lines: [] as OrderLineForm[],
});

const filteredServiceLevels = computed(() => {
  return allServiceLevels.value;
});

const grandTotal = computed(() => {
  return form.lines.reduce((sum, line) => sum + (line.orderedQuantity || 0) * (line.unitPrice || 0), 0);
});

async function loadDropdowns(): Promise<void> {
  customersLoading.value = true;
  warehousesLoading.value = true;
  productsLoading.value = true;
  carriersLoading.value = true;
  try {
    const [custRes, whRes, prodRes, carrRes] = await Promise.all([
      searchCustomers({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchWarehouses({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchProducts({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchCarriers({ isActive: true, sortDescending: false, page: 1, pageSize: 1000 }),
    ]);
    customers.value = custRes.items;
    warehouses.value = whRes.items;
    products.value = prodRes.items;
    carriers.value = carrRes.items;
  } catch {
    // silent
  } finally {
    customersLoading.value = false;
    warehousesLoading.value = false;
    productsLoading.value = false;
    carriersLoading.value = false;
  }
}

onMounted(() => loadDropdowns());

async function onCarrierChanged(): Promise<void> {
  form.carrierServiceLevelId = null;
  allServiceLevels.value = [];
  if (!form.carrierId) return;
  try {
    const detail = await getCarrierById(form.carrierId);
    allServiceLevels.value = detail.serviceLevels;
  } catch {
    allServiceLevels.value = [];
  }
}

async function populateForm(): Promise<void> {
  if (visible.value && props.soId) {
    isEdit.value = true;
    try {
      const detail: SalesOrderDetailDto = await getSalesOrderById(props.soId);
      form.customerId = detail.customerId;
      form.warehouseId = detail.warehouseId;
      form.requestedShipDate = detail.requestedShipDate ? detail.requestedShipDate.substring(0, 10) : '';
      form.carrierId = detail.carrierId;
      form.carrierServiceLevelId = detail.carrierServiceLevelId;
      form.shippingStreetLine1 = detail.shippingStreetLine1;
      form.shippingStreetLine2 = detail.shippingStreetLine2 ?? '';
      form.shippingCity = detail.shippingCity;
      form.shippingStateProvince = detail.shippingStateProvince ?? '';
      form.shippingPostalCode = detail.shippingPostalCode;
      form.shippingCountryCode = detail.shippingCountryCode;
      form.notes = detail.notes ?? '';
      form.lines = [];
      if (detail.carrierId) {
        try {
          const carrierDetail = await getCarrierById(detail.carrierId);
          allServiceLevels.value = carrierDetail.serviceLevels;
        } catch {
          allServiceLevels.value = [];
        }
      }
    } catch (err) {
      notification.error(getApiErrorMessage(err, t));
    }
  } else if (visible.value) {
    isEdit.value = false;
    form.customerId = null;
    form.warehouseId = null;
    form.requestedShipDate = '';
    form.carrierId = null;
    form.carrierServiceLevelId = null;
    form.shippingStreetLine1 = '';
    form.shippingStreetLine2 = '';
    form.shippingCity = '';
    form.shippingStateProvince = '';
    form.shippingPostalCode = '';
    form.shippingCountryCode = '';
    form.notes = '';
    form.lines = [];
    allServiceLevels.value = [];
  }
  fieldErrors.customerId = [];
}

watch(visible, populateForm);
watch(() => props.soId, populateForm);

function addLine(): void {
  form.lines.push({ productId: null, orderedQuantity: 1, unitPrice: 0, notes: '' });
}

function removeLine(index: number): void {
  form.lines.splice(index, 1);
}

function onShipDatePicked(value: unknown): void {
  if (value instanceof Date) {
    form.requestedShipDate = value.toISOString().split('T')[0];
  } else {
    form.requestedShipDate = '';
  }
  shipDateMenu.value = false;
}

const rules = {
  required: (v: string) => !!v || t('common.required'),
  requiredSelect: (v: number | null) => v !== null || t('common.required'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
  streetLength: (v: string) => !v || v.length <= 200 || t('validation.streetLength'),
  postalCodeLength: (v: string) => !v || v.length <= 20 || t('validation.postalCodeLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  if (!isEdit.value && form.lines.length === 0) {
    notification.error(t('salesOrders.detail.noLines'));
    return;
  }

  if (!isEdit.value) {
    const invalidLines = form.lines.some(
      (l) => !l.productId || !l.orderedQuantity || l.orderedQuantity <= 0 || l.unitPrice < 0,
    );
    if (invalidLines) {
      notification.error(t('errors.VALIDATION_ERROR'));
      return;
    }
  }

  loading.value = true;
  try {
    if (isEdit.value && props.soId) {
      await updateSalesOrder(props.soId, {
        warehouseId: form.warehouseId!,
        requestedShipDate: form.requestedShipDate || null,
        carrierId: form.carrierId,
        carrierServiceLevelId: form.carrierServiceLevelId,
        shippingStreetLine1: form.shippingStreetLine1,
        shippingStreetLine2: form.shippingStreetLine2 || null,
        shippingCity: form.shippingCity,
        shippingStateProvince: form.shippingStateProvince || null,
        shippingPostalCode: form.shippingPostalCode,
        shippingCountryCode: form.shippingCountryCode,
        notes: form.notes || null,
      });
      notification.success(t('salesOrders.edit') + ' \u2713');
    } else {
      await createSalesOrder({
        customerId: form.customerId!,
        warehouseId: form.warehouseId!,
        requestedShipDate: form.requestedShipDate || null,
        carrierId: form.carrierId,
        carrierServiceLevelId: form.carrierServiceLevelId,
        shippingStreetLine1: form.shippingStreetLine1,
        shippingStreetLine2: form.shippingStreetLine2 || null,
        shippingCity: form.shippingCity,
        shippingStateProvince: form.shippingStateProvince || null,
        shippingPostalCode: form.shippingPostalCode,
        shippingCountryCode: form.shippingCountryCode,
        notes: form.notes || null,
        lines: form.lines.map((l) => ({
          productId: l.productId!,
          orderedQuantity: l.orderedQuantity,
          unitPrice: l.unitPrice,
          notes: l.notes || null,
        })),
      });
      notification.success(t('salesOrders.create') + ' \u2713');
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
  if (errorCode === 'CUSTOMER_NOT_FOUND') {
    fieldErrors.customerId = [err.response?.data?.detail || t('errors.CUSTOMER_NOT_FOUND')];
  } else {
    notification.error(getApiErrorMessage(err, t));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
