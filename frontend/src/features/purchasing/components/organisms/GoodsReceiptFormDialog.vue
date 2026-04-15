<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="1100" :title="t('goodsReceipts.create')" icon="mdi-package-down" @back="cancel">
    <v-card-title class="text-h6">
      {{ t('goodsReceipts.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.purchaseOrderId"
              :label="t('goodsReceipts.form.purchaseOrder')"
              prepend-inner-icon="mdi-file-document"
              :density="layout.vuetifyDensity"
              :items="purchaseOrders"
              :item-title="poItemTitle"
              item-value="id"
              :loading="posLoading"
              :rules="[rules.requiredSelect]"
              @update:model-value="onPurchaseOrderSelected"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.warehouseId"
              :label="t('goodsReceipts.form.warehouse')"
              prepend-inner-icon="mdi-warehouse"
              :density="layout.vuetifyDensity"
              :items="warehouses"
              item-title="name"
              item-value="id"
              :loading="warehousesLoading"
              :rules="[rules.requiredSelect]"
              @update:model-value="onWarehouseChanged"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-select
              v-model="form.locationId"
              :label="t('goodsReceipts.form.location')"
              prepend-inner-icon="mdi-map-marker"
              :density="layout.vuetifyDensity"
              :items="locations"
              item-title="name"
              item-value="id"
              clearable
              :loading="locationsLoading"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('goodsReceipts.form.notes')"
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
          <div class="text-subtitle-1 font-weight-medium">{{ t('goodsReceipts.detail.lines') }}</div>
        </div>

        <v-table v-if="form.lines.length > 0" :density="layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ t('goodsReceipts.lines.product') }}</th>
              <th>{{ t('goodsReceipts.lines.remaining') }}</th>
              <th>{{ t('goodsReceipts.lines.receivedQuantity') }}</th>
              <th>{{ t('goodsReceipts.lines.batchNumber') }}</th>
              <th>{{ t('goodsReceipts.lines.manufacturingDate') }}</th>
              <th>{{ t('goodsReceipts.lines.expiryDate') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(line, idx) in form.lines" :key="idx">
              <td>{{ line.productName }}</td>
              <td>{{ line.remainingQuantity }}</td>
              <td style="min-width: 100px">
                <v-text-field
                  v-model.number="line.receivedQuantity"
                  type="number"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  :min="0"
                  :max="line.remainingQuantity"
                  step="any"
                />
              </td>
              <td style="min-width: 160px">
                <v-text-field
                  :model-value="line.batchNumberPreview"
                  :density="layout.vuetifyDensity"
                  hide-details
                  variant="underlined"
                  readonly
                  disabled
                  prepend-inner-icon="mdi-barcode"
                />
              </td>
              <td style="min-width: 160px">
                <v-menu v-model="line.mfgDateMenu" :close-on-content-click="false">
                  <template #activator="{ props: menuProps }">
                    <v-text-field
                      v-bind="menuProps"
                      :model-value="line.manufacturingDate"
                      :density="layout.vuetifyDensity"
                      hide-details
                      variant="underlined"
                      prepend-inner-icon="mdi-calendar"
                      readonly
                      clearable
                      @click:clear="line.manufacturingDate = ''"
                    />
                  </template>
                  <v-date-picker
                    :model-value="line.manufacturingDate ? new Date(line.manufacturingDate + 'T00:00:00') : undefined"
                    color="primary"
                    show-adjacent-months
                    @update:model-value="(v: unknown) => { line.manufacturingDate = formatDateValue(v); line.mfgDateMenu = false; }"
                  />
                </v-menu>
              </td>
              <td style="min-width: 160px">
                <v-menu v-model="line.expDateMenu" :close-on-content-click="false">
                  <template #activator="{ props: menuProps }">
                    <v-text-field
                      v-bind="menuProps"
                      :model-value="line.expiryDate"
                      :density="layout.vuetifyDensity"
                      hide-details
                      variant="underlined"
                      prepend-inner-icon="mdi-calendar"
                      readonly
                      clearable
                      @click:clear="line.expiryDate = ''"
                    />
                  </template>
                  <v-date-picker
                    :model-value="line.expiryDate ? new Date(line.expiryDate + 'T00:00:00') : undefined"
                    color="primary"
                    show-adjacent-months
                    @update:model-value="(v: unknown) => { line.expiryDate = formatDateValue(v); line.expDateMenu = false; }"
                  />
                </v-menu>
              </td>
            </tr>
          </tbody>
        </v-table>

        <div v-else class="text-center text-medium-emphasis py-4">
          {{ t('goodsReceipts.detail.selectPo') }}
        </div>
      </v-form>
    </v-card-text>

    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="cancel">{{ t('common.cancel') }}</v-btn>
      <v-btn color="primary" variant="flat" :loading="loading" :disabled="form.lines.length === 0" @click="handleSubmit">
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
import { createGoodsReceipt } from '@features/purchasing/api/goods-receipts';
import { searchPurchaseOrders, getPurchaseOrderById } from '@features/purchasing/api/purchase-orders';
import { searchWarehouses } from '@features/inventory/api/warehouses';
import { searchLocations } from '@features/inventory/api/storage-locations';
import type {
  PurchaseOrderDto,
  PurchaseOrderDetailDto,
  PurchaseOrderLineDto,
} from '@features/purchasing/types/purchasing';
import type { WarehouseDto, StorageLocationDto } from '@features/inventory/types/inventory';

interface ReceiptLineForm {
  purchaseOrderLineId: number;
  productName: string;
  productCode: string;
  remainingQuantity: number;
  receivedQuantity: number;
  batchNumberPreview: string;
  manufacturingDate: string;
  expiryDate: string;
  mfgDateMenu: boolean;
  expDateMenu: boolean;
}

function formatDateValue(date: unknown): string {
  if (date instanceof Date) {
    return date.toISOString().split('T')[0];
  }
  return '';
}

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  preSelectedPoId?: number | null;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const formRef = ref();
const loading = ref(false);
const purchaseOrders = ref<PurchaseOrderDto[]>([]);
const posLoading = ref(false);
const warehouses = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const locations = ref<StorageLocationDto[]>([]);
const locationsLoading = ref(false);
const selectedPoDetail = ref<PurchaseOrderDetailDto | null>(null);

const form = reactive({
  purchaseOrderId: null as number | null,
  warehouseId: null as number | null,
  locationId: null as number | null,
  notes: '',
  lines: [] as ReceiptLineForm[],
});

function poItemTitle(item: PurchaseOrderDto): string {
  return `${item.orderNumber} - ${item.supplierName}`;
}

async function loadDropdowns(): Promise<void> {
  posLoading.value = true;
  warehousesLoading.value = true;
  try {
    const [poRes, whRes] = await Promise.all([
      searchPurchaseOrders({ status: 'Confirmed', sortDescending: true, page: 1, pageSize: 500 }),
      searchWarehouses({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
    ]);
    const partialRes = await searchPurchaseOrders({ status: 'PartiallyReceived', sortDescending: true, page: 1, pageSize: 500 });
    purchaseOrders.value = [...poRes.items, ...partialRes.items];
    warehouses.value = whRes.items;
  } catch {
    // silent
  } finally {
    posLoading.value = false;
    warehousesLoading.value = false;
  }
}

onMounted(async () => {
  await loadDropdowns();
  if (props.preSelectedPoId) {
    form.purchaseOrderId = props.preSelectedPoId;
    await onPurchaseOrderSelected();
  }
});

watch(visible, (val) => {
  if (val) {
    form.purchaseOrderId = props.preSelectedPoId ?? null;
    form.warehouseId = null;
    form.locationId = null;
    form.notes = '';
    form.lines = [];
    selectedPoDetail.value = null;
    locations.value = [];
    if (props.preSelectedPoId) {
      onPurchaseOrderSelected();
    }
  }
});

async function onPurchaseOrderSelected(): Promise<void> {
  form.lines = [];
  selectedPoDetail.value = null;
  if (!form.purchaseOrderId) return;

  try {
    const detail = await getPurchaseOrderById(form.purchaseOrderId);
    selectedPoDetail.value = detail;
    form.warehouseId = detail.destinationWarehouseId;
    await onWarehouseChanged();

    const receivableLines = detail.lines.filter((l: PurchaseOrderLineDto) => l.remainingQuantity > 0);
    const dateSegment = new Date().toISOString().slice(0, 7).replace('-', '');

    form.lines = receivableLines.map((l: PurchaseOrderLineDto) => ({
      purchaseOrderLineId: l.id,
      productName: l.productName,
      productCode: l.productCode,
      remainingQuantity: l.remainingQuantity,
      receivedQuantity: 0,
      batchNumberPreview: `${l.productCode}-${dateSegment}-###`,
      manufacturingDate: '',
      expiryDate: '',
      mfgDateMenu: false,
      expDateMenu: false,
    }));
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  }
}

async function onWarehouseChanged(): Promise<void> {
  form.locationId = null;
  locations.value = [];
  if (!form.warehouseId) return;

  locationsLoading.value = true;
  try {
    const response = await searchLocations({ warehouseId: form.warehouseId, page: 1, pageSize: 1000 });
    locations.value = response.items;
  } catch {
    locations.value = [];
  } finally {
    locationsLoading.value = false;
  }
}

const rules = {
  requiredSelect: (v: number | null) => v !== null || t('common.required'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  const activeLines = form.lines.filter((l) => l.receivedQuantity > 0);

  if (activeLines.length === 0) {
    notification.error(t('goodsReceipts.detail.noLines'));
    return;
  }

  const invalidLines = activeLines.some(
    (l) => l.receivedQuantity > l.remainingQuantity
  );
  if (invalidLines) {
    notification.error(t('errors.VALIDATION_ERROR'));
    return;
  }

  loading.value = true;
  try {
    await createGoodsReceipt({
      purchaseOrderId: form.purchaseOrderId!,
      warehouseId: form.warehouseId!,
      locationId: form.locationId ?? undefined,
      notes: form.notes || null,
      lines: activeLines.map((l) => ({
        purchaseOrderLineId: l.purchaseOrderLineId,
        receivedQuantity: l.receivedQuantity,
        manufacturingDate: l.manufacturingDate || null,
        expiryDate: l.expiryDate || null,
      })),
    });
    notification.success(t('goodsReceipts.create') + ' \u2713');
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
