<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.order">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.order.orderNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.order.customerName }}</div>
        </div>
        <v-spacer />
        <v-chip :color="soStatusColor(vm.order.status)" size="small" label class="mr-2">
          {{ vm.t(`salesOrders.status.${vm.order.status}`) }}
        </v-chip>
        <v-btn v-if="vm.order.status === 'Draft'" color="primary" variant="tonal" prepend-icon="mdi-pencil" class="mr-2" @click="handleEdit">
          {{ vm.t('common.edit') }}
        </v-btn>
        <v-btn v-if="vm.order.status === 'Draft'" color="success" variant="tonal" prepend-icon="mdi-check" class="mr-2" @click="showConfirmDialog = true">
          {{ vm.t('salesOrders.confirm') }}
        </v-btn>
        <v-btn v-if="vm.order.status === 'Draft' || vm.order.status === 'Confirmed'" color="error" variant="tonal" prepend-icon="mdi-close" class="mr-2" @click="showCancelDialog = true">
          {{ vm.t('salesOrders.cancel') }}
        </v-btn>
        <v-btn v-if="vm.order.status === 'Confirmed' || vm.order.status === 'Picking'" color="amber" variant="tonal" prepend-icon="mdi-clipboard-list" class="mr-2" @click="showGeneratePickListDialog = true">
          {{ vm.t('salesOrders.generatePickList') }}
        </v-btn>
        <v-btn v-if="vm.order.status === 'Packed'" color="teal" variant="tonal" prepend-icon="mdi-truck-fast" class="mr-2" @click="showDispatchDialog = true">
          {{ vm.t('salesOrders.dispatch') }}
        </v-btn>
        <v-btn v-if="vm.order.status === 'Shipped'" color="green" variant="tonal" prepend-icon="mdi-check-all" @click="showCompleteDialog = true">
          {{ vm.t('salesOrders.complete') }}
        </v-btn>
      </div>

      <!-- SO Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('salesOrders.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.customerName') }}</div>
              <div>{{ vm.order.customerName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.warehouseName') }}</div>
              <div>{{ vm.order.warehouseName }}</div>
            </v-col>
            <v-col v-if="vm.order.carrierName" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.carrier') }}</div>
              <div>{{ vm.order.carrierName }}{{ vm.order.carrierServiceLevelName ? ' - ' + vm.order.carrierServiceLevelName : '' }}</div>
            </v-col>
            <v-col v-if="vm.order.requestedShipDate" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.requestedShipDate') }}</div>
              <div>{{ vm.formatDate(vm.order.requestedShipDate) }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.totalAmount') }}</div>
              <div>{{ vm.order.totalAmount.toFixed(2) }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.order.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.order.confirmedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.confirmedAt') }}</div>
              <div>{{ vm.formatDate(vm.order.confirmedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.order.shippedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shippedAt') }}</div>
              <div>{{ vm.formatDate(vm.order.shippedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.order.completedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.completedAt') }}</div>
              <div>{{ vm.formatDate(vm.order.completedAtUtc) }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Shipping Address Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-map-marker" class="mr-2" />
          {{ vm.t('salesOrders.detail.shippingAddress') }}
        </v-card-title>
        <v-card-text>
          <div>{{ vm.order.shippingStreetLine1 }}</div>
          <div v-if="vm.order.shippingStreetLine2">{{ vm.order.shippingStreetLine2 }}</div>
          <div>{{ vm.order.shippingCity }}{{ vm.order.shippingStateProvince ? ', ' + vm.order.shippingStateProvince : '' }} {{ vm.order.shippingPostalCode }}</div>
          <div>{{ vm.order.shippingCountryCode }}</div>
        </v-card-text>
      </v-card>

      <!-- Notes Card -->
      <v-card v-if="vm.order.notes" :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-note-text" class="mr-2" />
          {{ vm.t('salesOrders.form.notes') }}
        </v-card-title>
        <v-card-text>
          <div class="text-body-2">{{ vm.order.notes }}</div>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('salesOrders.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.order.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('salesOrders.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('salesOrders.lines.product') }}</th>
              <th class="text-end">{{ vm.t('salesOrders.lines.orderedQty') }}</th>
              <th class="text-end">{{ vm.t('salesOrders.lines.pickedQty') }}</th>
              <th class="text-end">{{ vm.t('salesOrders.lines.packedQty') }}</th>
              <th class="text-end">{{ vm.t('salesOrders.lines.shippedQty') }}</th>
              <th class="text-end">{{ vm.t('salesOrders.lines.unitPrice') }}</th>
              <th class="text-end">{{ vm.t('salesOrders.lines.lineTotal') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.order.lines" :key="line.id">
              <td>{{ line.productCode }} - {{ line.productName }}</td>
              <td class="text-end">{{ line.orderedQuantity }}</td>
              <td class="text-end">{{ line.pickedQuantity }}</td>
              <td class="text-end">{{ line.packedQuantity }}</td>
              <td class="text-end">{{ line.shippedQuantity }}</td>
              <td class="text-end">{{ line.unitPrice.toFixed(2) }}</td>
              <td class="text-end">{{ line.lineTotal.toFixed(2) }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>

      <!-- Tabs for Pick Lists, Parcels, Shipment -->
      <v-card>
        <v-tabs v-model="activeTab" color="primary">
          <v-tab value="pickLists">{{ vm.t('salesOrders.tabs.pickLists') }}</v-tab>
          <v-tab value="parcels">{{ vm.t('salesOrders.tabs.parcels') }}</v-tab>
          <v-tab value="shipment">{{ vm.t('salesOrders.tabs.shipment') }}</v-tab>
        </v-tabs>

        <v-tabs-window v-model="activeTab">
          <!-- Pick Lists Tab -->
          <v-tabs-window-item value="pickLists">
            <v-card-text v-if="vm.order.pickLists.length === 0" class="text-medium-emphasis">
              {{ vm.t('salesOrders.detail.noPickLists') }}
            </v-card-text>
            <v-table v-else :density="vm.layout.vuetifyDensity">
              <thead>
                <tr>
                  <th>{{ vm.t('pickLists.columns.pickListNumber') }}</th>
                  <th>{{ vm.t('pickLists.columns.status') }}</th>
                  <th>{{ vm.t('pickLists.columns.createdAt') }}</th>
                  <th>{{ vm.t('common.actions') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="pl in vm.order.pickLists" :key="pl.id">
                  <td>{{ pl.pickListNumber }}</td>
                  <td>
                    <v-chip :color="pickListStatusColor(pl.status)" size="small" label>
                      {{ vm.t(`pickLists.status.${pl.status}`) }}
                    </v-chip>
                  </td>
                  <td>{{ vm.formatDate(pl.createdAtUtc) }}</td>
                  <td>
                    <ActionChip :label="vm.t('common.view')" icon="mdi-eye" color="info" :compact="vm.layout.isCompact" @click="vm.navigateToPickList(pl.id)" />
                  </td>
                </tr>
              </tbody>
            </v-table>
          </v-tabs-window-item>

          <!-- Parcels Tab -->
          <v-tabs-window-item value="parcels">
            <div class="d-flex align-center pa-4">
              <v-spacer />
              <v-btn v-if="vm.order.status === 'Picking' || vm.order.status === 'Packed'" size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showParcelFormDialog = true">
                {{ vm.t('salesOrders.detail.addParcel') }}
              </v-btn>
            </div>
            <v-card-text v-if="vm.order.parcels.length === 0" class="text-medium-emphasis pt-0">
              {{ vm.t('salesOrders.detail.noParcels') }}
            </v-card-text>
            <div v-else class="px-4 pb-4">
              <v-card v-for="parcel in vm.order.parcels" :key="parcel.id" variant="outlined" class="mb-2">
                <v-card-title class="text-subtitle-2 d-flex align-center">
                  <v-icon icon="mdi-package-variant" class="mr-2" size="small" />
                  {{ parcel.parcelNumber }}
                  <v-spacer />
                  <v-btn icon="mdi-plus" size="x-small" variant="text" color="primary" @click="openParcelItemDialog(parcel)" />
                  <v-btn icon="mdi-pencil" size="x-small" variant="text" color="primary" @click="openEditParcelDialog(parcel)" />
                  <v-btn icon="mdi-delete" size="x-small" variant="text" color="error" @click="vm.handleDeleteParcel(parcel.id)" />
                </v-card-title>
                <v-card-text>
                  <div v-if="parcel.weightKg || parcel.lengthCm" class="text-caption text-medium-emphasis mb-2">
                    <span v-if="parcel.weightKg">{{ vm.t('salesOrders.parcel.weight') }}: {{ parcel.weightKg }} kg</span>
                    <span v-if="parcel.lengthCm"> | {{ parcel.lengthCm }} x {{ parcel.widthCm }} x {{ parcel.heightCm }} cm</span>
                    <span v-if="parcel.trackingNumber"> | {{ vm.t('salesOrders.parcel.tracking') }}: {{ parcel.trackingNumber }}</span>
                  </div>
                  <v-table v-if="parcel.items.length > 0" density="compact">
                    <thead>
                      <tr>
                        <th>{{ vm.t('salesOrders.lines.product') }}</th>
                        <th class="text-end">{{ vm.t('salesOrders.parcel.quantity') }}</th>
                        <th style="width: 60px"></th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="item in parcel.items" :key="item.id">
                        <td>{{ item.productCode }} - {{ item.productName }}</td>
                        <td class="text-end">{{ item.quantity }}</td>
                        <td>
                          <v-btn icon="mdi-delete" size="x-small" variant="text" color="error" @click="vm.handleRemoveParcelItem(parcel.id, item.id)" />
                        </td>
                      </tr>
                    </tbody>
                  </v-table>
                  <div v-else class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.parcel.noItems') }}</div>
                </v-card-text>
              </v-card>
            </div>
          </v-tabs-window-item>

          <!-- Shipment Tab -->
          <v-tabs-window-item value="shipment">
            <v-card-text v-if="!vm.order.shipment" class="text-medium-emphasis">
              {{ vm.t('salesOrders.detail.noShipment') }}
            </v-card-text>
            <v-card-text v-else>
              <v-row dense>
                <v-col cols="12" md="6">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.columns.shipmentNumber') }}</div>
                  <div>
                    <a class="text-primary cursor-pointer" @click="vm.navigateToShipment(vm.order.shipment!.id)">{{ vm.order.shipment.shipmentNumber }}</a>
                  </div>
                </v-col>
                <v-col cols="12" md="6">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.columns.status') }}</div>
                  <div>
                    <v-chip :color="shipmentStatusColor(vm.order.shipment.status)" size="small" label>
                      {{ vm.t(`shipments.status.${vm.order.shipment.status}`) }}
                    </v-chip>
                  </div>
                </v-col>
                <v-col cols="12" md="6">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.columns.dispatchedAt') }}</div>
                  <div>{{ vm.formatDate(vm.order.shipment.dispatchedAtUtc) }}</div>
                </v-col>
              </v-row>
            </v-card-text>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-card>
    </template>

    <!-- Confirm Dialog -->
    <ConfirmDialog
      v-model="showConfirmDialog"
      :title="vm.t('salesOrders.confirm')"
      :message="vm.t('salesOrders.confirmMessage', { orderNumber: vm.order?.orderNumber })"
      :confirm-text="vm.t('salesOrders.confirm')"
      color="success"
      icon="mdi-check"
      :loading="confirming"
      @confirm="handleConfirm"
    />

    <!-- Cancel Dialog -->
    <ConfirmDialog
      v-model="showCancelDialog"
      :title="vm.t('salesOrders.cancel')"
      :message="vm.t('salesOrders.cancelMessage', { orderNumber: vm.order?.orderNumber })"
      :confirm-text="vm.t('salesOrders.cancel')"
      color="error"
      icon="mdi-close"
      :loading="cancelling"
      @confirm="handleCancel"
    />

    <!-- Complete Dialog -->
    <ConfirmDialog
      v-model="showCompleteDialog"
      :title="vm.t('salesOrders.complete')"
      :message="vm.t('salesOrders.completeMessage', { orderNumber: vm.order?.orderNumber })"
      :confirm-text="vm.t('salesOrders.complete')"
      color="green"
      icon="mdi-check-all"
      :loading="completing"
      @confirm="handleComplete"
    />

    <!-- Generate Pick List Dialog -->
    <ConfirmDialog
      v-model="showGeneratePickListDialog"
      :title="vm.t('salesOrders.generatePickList')"
      :message="vm.t('salesOrders.generatePickListMessage', { orderNumber: vm.order?.orderNumber })"
      :confirm-text="vm.t('salesOrders.generatePickList')"
      color="amber"
      icon="mdi-clipboard-list"
      :loading="generatingPickList"
      @confirm="handleGeneratePickList"
    />

    <!-- Dispatch Dialog -->
    <DispatchDialog
      v-model="showDispatchDialog"
      :sales-order="vm.order"
      @dispatched="onDispatched"
    />

    <!-- Parcel Form Dialog -->
    <ParcelFormDialog
      v-model="showParcelFormDialog"
      :parcel="editingParcel"
      @saved="onParcelSaved"
    />

    <!-- Parcel Item Dialog -->
    <ParcelItemDialog
      v-model="showParcelItemDialog"
      :so-lines="vm.order?.lines ?? []"
      :pick-lines="vm.confirmedPickLines"
      @saved="onParcelItemSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { useSalesOrderDetailView } from '@features/fulfillment/composables/useSalesOrderDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import DispatchDialog from '@features/fulfillment/components/organisms/DispatchDialog.vue';
import ParcelFormDialog from '@features/fulfillment/components/organisms/ParcelFormDialog.vue';
import ParcelItemDialog from '@features/fulfillment/components/organisms/ParcelItemDialog.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { ParcelDto } from '@features/fulfillment/types/fulfillment';

const router = useRouter();
const notification = useNotificationStore();
const vm = reactive(useSalesOrderDetailView());

const activeTab = ref('pickLists');

const showConfirmDialog = ref(false);
const showCancelDialog = ref(false);
const showCompleteDialog = ref(false);
const showGeneratePickListDialog = ref(false);
const showDispatchDialog = ref(false);
const showParcelFormDialog = ref(false);
const showParcelItemDialog = ref(false);

const confirming = ref(false);
const cancelling = ref(false);
const completing = ref(false);
const generatingPickList = ref(false);

const editingParcel = ref<ParcelDto | null>(null);
const activeParcelId = ref<number | null>(null);

function soStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    Picking: 'amber',
    Packed: 'indigo',
    Shipped: 'teal',
    Completed: 'green',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}

function pickListStatusColor(status: string): string {
  const map: Record<string, string> = {
    Pending: 'grey',
    Completed: 'green',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}

function shipmentStatusColor(status: string): string {
  const map: Record<string, string> = {
    Dispatched: 'blue',
    InTransit: 'teal',
    OutForDelivery: 'amber',
    Delivered: 'green',
    Failed: 'red',
    Returned: 'orange',
  };
  return map[status] || 'grey';
}

function handleEdit(): void {
  router.push({ name: 'sales-order-edit', params: { id: vm.orderId } });
}

function openEditParcelDialog(parcel: ParcelDto): void {
  editingParcel.value = parcel;
  showParcelFormDialog.value = true;
}

function openParcelItemDialog(parcel: ParcelDto): void {
  activeParcelId.value = parcel.id;
  showParcelItemDialog.value = true;
}

async function handleConfirm(): Promise<void> {
  confirming.value = true;
  try {
    await vm.handleConfirm();
    showConfirmDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    confirming.value = false;
  }
}

async function handleCancel(): Promise<void> {
  cancelling.value = true;
  try {
    await vm.handleCancel();
    showCancelDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    cancelling.value = false;
  }
}

async function handleComplete(): Promise<void> {
  completing.value = true;
  try {
    await vm.handleComplete();
    showCompleteDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    completing.value = false;
  }
}

async function handleGeneratePickList(): Promise<void> {
  generatingPickList.value = true;
  try {
    await vm.handleGeneratePickList();
    showGeneratePickListDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    generatingPickList.value = false;
  }
}

async function onDispatched(): Promise<void> {
  showDispatchDialog.value = false;
  await vm.loadSalesOrder();
}

async function onParcelSaved(): Promise<void> {
  showParcelFormDialog.value = false;
  editingParcel.value = null;
  await vm.loadSalesOrder();
}

async function onParcelItemSaved(): Promise<void> {
  showParcelItemDialog.value = false;
  activeParcelId.value = null;
  await vm.loadSalesOrder();
}
</script>
