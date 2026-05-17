<template>
  <div class="so-detail">
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.order">
      <!-- Header row (read-only: back, number/customer, status) -->
      <div class="d-flex align-center" :class="cardSpacing">
        <v-btn icon="mdi-arrow-left" variant="text" size="small" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-subtitle-1 font-weight-medium">{{ vm.order.orderNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.order.customerName ?? `#${vm.order.customerId}` }}</div>
        </div>
        <v-spacer />
        <v-chip :color="soStatusColor(vm.order.status)" size="small" label>
          {{ vm.t(`salesOrders.status.${vm.order.status}`) }}
        </v-chip>
      </div>

      <!-- Info card -->
      <v-card :class="cardSpacing" :density="vm.layout.vuetifyDensity">
        <v-card-title class="so-section-title">
          <v-icon icon="mdi-information" size="small" class="mr-1" />
          {{ vm.t('salesOrders.detail.info') }}
        </v-card-title>
        <v-card-text :class="cardPadding">
          <v-row dense>
            <v-col cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.customerName') }}</div>
              <div class="text-body-2">{{ vm.order.customerName ?? `#${vm.order.customerId}` }}</div>
            </v-col>
            <v-col cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.warehouseName') }}</div>
              <div class="text-body-2">{{ vm.order.warehouseName ?? `#${vm.order.warehouseId}` }}</div>
            </v-col>
            <v-col v-if="vm.order.carrierName || vm.order.carrierId" cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.carrier') }}</div>
              <div class="text-body-2">{{ vm.order.carrierName ?? `#${vm.order.carrierId}` }}{{ vm.order.carrierServiceLevelName ? ' - ' + vm.order.carrierServiceLevelName : '' }}</div>
            </v-col>
            <v-col v-if="vm.order.requestedShipDate" cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.requestedShipDate') }}</div>
              <div class="text-body-2">{{ vm.formatDate(vm.order.requestedShipDate) }}</div>
            </v-col>
            <v-col cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.totalAmount') }}</div>
              <div class="text-body-2 font-weight-medium">{{ vm.order.totalAmount.toFixed(2) }} {{ vm.order.currencyCode }}</div>
            </v-col>
            <v-col cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.columns.createdAt') }}</div>
              <div class="text-body-2">{{ vm.formatDate(vm.order.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.order.confirmedAtUtc" cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.confirmedAt') }}</div>
              <div class="text-body-2">{{ vm.formatDate(vm.order.confirmedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.order.shippedAtUtc" cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shippedAt') }}</div>
              <div class="text-body-2">{{ vm.formatDate(vm.order.shippedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.order.completedAtUtc" cols="12" sm="6" md="4">
              <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.completedAt') }}</div>
              <div class="text-body-2">{{ vm.formatDate(vm.order.completedAtUtc) }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Billing | Shipping (2 cols) -->
      <v-row dense :class="cardSpacing">
        <v-col cols="12" md="6">
          <v-card class="h-100" :density="vm.layout.vuetifyDensity">
            <v-card-title class="so-section-title">
              <v-icon icon="mdi-credit-card-outline" size="small" class="mr-1" />
              {{ vm.t('salesOrders.form.billingAddress') }}
            </v-card-title>
            <v-card-text :class="`${cardPadding} text-body-2`">
              <div>{{ vm.order.billingStreetLine1 }}</div>
              <div v-if="vm.order.billingStreetLine2">{{ vm.order.billingStreetLine2 }}</div>
              <div>{{ vm.order.billingCity }}{{ vm.order.billingStateProvince ? ', ' + vm.order.billingStateProvince : '' }} {{ vm.order.billingPostalCode }}</div>
              <div>{{ vm.order.billingCountryName ?? vm.order.billingCountryCode }}</div>
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="12" md="6">
          <v-card class="h-100" :density="vm.layout.vuetifyDensity">
            <v-card-title class="so-section-title">
              <v-icon icon="mdi-map-marker" size="small" class="mr-1" />
              {{ vm.t('salesOrders.detail.shippingAddress') }}
            </v-card-title>
            <v-card-text :class="`${cardPadding} text-body-2`">
              <div>{{ vm.order.shippingStreetLine1 }}</div>
              <div v-if="vm.order.shippingStreetLine2">{{ vm.order.shippingStreetLine2 }}</div>
              <div>{{ vm.order.shippingCity }}{{ vm.order.shippingStateProvince ? ', ' + vm.order.shippingStateProvince : '' }} {{ vm.order.shippingPostalCode }}</div>
              <div>{{ vm.order.shippingCountryName ?? vm.order.shippingCountryCode }}</div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <!-- Notes (full width, only when present) -->
      <v-card v-if="vm.order.notes" :class="cardSpacing" :density="vm.layout.vuetifyDensity">
        <v-card-title class="so-section-title">
          <v-icon icon="mdi-note-text" size="small" class="mr-1" />
          {{ vm.t('salesOrders.form.notes') }}
        </v-card-title>
        <v-card-text :class="`${cardPadding} text-body-2`">{{ vm.order.notes }}</v-card-text>
      </v-card>

      <!-- Lines (full width) -->
      <v-card :class="cardSpacing" :density="vm.layout.vuetifyDensity">
        <v-card-title class="so-section-title">
          <v-icon icon="mdi-format-list-bulleted" size="small" class="mr-1" />
          {{ vm.t('salesOrders.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="(vm.order.lines ?? []).length === 0" :class="`${cardPadding} text-caption text-medium-emphasis`">
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
            <tr v-for="line in (vm.order.lines ?? [])" :key="line.id">
              <td>{{ line.productCode ? `${line.productCode} - ${line.productName}` : `#${line.productId}` }}</td>
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

      <!-- Pick Lists (hidden when empty) -->
      <v-card v-if="(vm.order.pickLists ?? []).length > 0" :class="cardSpacing" :density="vm.layout.vuetifyDensity">
        <v-card-title class="so-section-title">
          <v-icon icon="mdi-clipboard-list" size="small" class="mr-1" />
          {{ vm.t('salesOrders.tabs.pickLists') }}
        </v-card-title>
        <v-table :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('pickLists.columns.pickListNumber') }}</th>
              <th>{{ vm.t('pickLists.columns.status') }}</th>
              <th>{{ vm.t('pickLists.columns.createdAt') }}</th>
              <th>{{ vm.t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="pl in (vm.order.pickLists ?? [])" :key="pl.id">
              <td>{{ pl.pickListNumber }}</td>
              <td>
                <v-chip :color="pickListStatusColor(pl.status)" size="small" label>
                  {{ vm.t(`pickLists.status.${pl.status}`) }}
                </v-chip>
              </td>
              <td>{{ vm.formatDate(pl.createdAtUtc) }}</td>
              <td>
                <v-btn
                  icon="mdi-open-in-new"
                  size="x-small"
                  variant="text"
                  color="info"
                  :title="vm.t('common.view')"
                  @click="vm.navigateToPickList(pl.id)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>

      <!-- Packing & Shipment (collapsible, hidden when empty) -->
      <v-card v-if="showPackingSection" :class="cardSpacing" :density="vm.layout.vuetifyDensity">
        <v-card-title class="so-section-title d-flex align-center cursor-pointer" @click="packingCollapsed = !packingCollapsed">
          <v-icon icon="mdi-package-variant-closed" size="small" class="mr-1" />
          {{ vm.t('salesOrders.detail.packingAndShipment') }}
          <v-spacer />
          <v-btn
            :icon="packingCollapsed ? 'mdi-chevron-down' : 'mdi-chevron-up'"
            variant="text"
            size="x-small"
            @click.stop="packingCollapsed = !packingCollapsed"
          />
        </v-card-title>

        <v-expand-transition>
          <div v-show="!packingCollapsed">
            <v-alert v-if="vm.order.shipment" :color="shipmentStatusColor(vm.order.shipment.status)" variant="tonal" density="compact" class="mx-3 mb-2">
              <div class="d-flex align-center flex-wrap mb-2" style="gap: 10px">
                <v-icon icon="mdi-truck-fast" size="small" />
                <a class="text-primary font-weight-medium cursor-pointer" @click="vm.navigateToShipment(vm.order.shipment!.id)">
                  {{ vm.order.shipment.shipmentNumber }}
                </a>
                <v-chip :color="shipmentStatusColor(vm.order.shipment.status)" size="x-small" label>
                  {{ vm.t(`shipments.status.${vm.order.shipment.status}`) }}
                </v-chip>
                <v-spacer />
                <v-btn
                  v-if="vm.order.shipment.trackingUrl"
                  :href="vm.order.shipment.trackingUrl"
                  target="_blank"
                  rel="noopener"
                  size="x-small"
                  variant="tonal"
                  prepend-icon="mdi-open-in-new"
                >
                  {{ vm.t('salesOrders.detail.shipmentOpenTracking') }}
                </v-btn>
              </div>
              <v-row dense class="shipment-grid">
                <v-col v-if="vm.order.shipment.carrierName" cols="12" sm="6" md="4">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shipmentCarrier') }}</div>
                  <div class="text-body-2">{{ vm.order.shipment.carrierName }}{{ vm.order.shipment.carrierServiceLevelName ? ' - ' + vm.order.shipment.carrierServiceLevelName : '' }}</div>
                </v-col>
                <v-col cols="12" sm="6" md="4">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shipmentDispatched') }}</div>
                  <div class="text-body-2">{{ vm.formatDate(vm.order.shipment.dispatchedAtUtc) }}</div>
                </v-col>
                <v-col v-if="vm.order.shipment.trackingNumber" cols="12" sm="6" md="4">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shipmentTracking') }}</div>
                  <div class="text-body-2">{{ vm.order.shipment.trackingNumber }}</div>
                </v-col>
                <v-col v-if="vm.order.shipment.lineCount !== undefined" cols="6" sm="3" md="2">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shipmentLineCount') }}</div>
                  <div class="text-body-2">{{ vm.order.shipment.lineCount }}</div>
                </v-col>
                <v-col v-if="vm.order.shipment.totalQuantity !== undefined" cols="6" sm="3" md="2">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shipmentTotalQuantity') }}</div>
                  <div class="text-body-2">{{ vm.order.shipment.totalQuantity }}</div>
                </v-col>
                <v-col v-if="vm.order.shipment.notes" cols="12">
                  <div class="text-caption text-medium-emphasis">{{ vm.t('salesOrders.detail.shipmentNotes') }}</div>
                  <div class="text-body-2">{{ vm.order.shipment.notes }}</div>
                </v-col>
              </v-row>
            </v-alert>

            <v-card-text v-if="(vm.order.parcels ?? []).length === 0" :class="`${cardPadding} text-caption text-medium-emphasis`">
              {{ vm.t('salesOrders.detail.noParcels') }}
            </v-card-text>
            <v-table v-else :density="vm.layout.vuetifyDensity">
              <thead>
                <tr>
                  <th>{{ vm.t('salesOrders.tabs.parcels') }}</th>
                  <th class="text-end">{{ vm.t('salesOrders.parcel.weight') }}</th>
                  <th>{{ vm.t('salesOrders.parcel.tracking') }}</th>
                  <th class="text-end">{{ vm.t('salesOrders.parcel.quantity') }}</th>
                  <th>{{ vm.t('salesOrders.lines.product') }}</th>
                </tr>
              </thead>
              <tbody v-for="parcel in (vm.order.parcels ?? [])" :key="parcel.id">
                <tr v-if="(parcel.items ?? []).length === 0">
                  <td>{{ parcel.parcelNumber }}</td>
                  <td class="text-end">{{ parcel.weightKg ? `${parcel.weightKg} kg` : '—' }}</td>
                  <td>{{ parcel.trackingNumber ?? '—' }}</td>
                  <td class="text-end text-medium-emphasis" colspan="2">{{ vm.t('salesOrders.parcel.noItems') }}</td>
                </tr>
                <tr v-for="(item, idx) in (parcel.items ?? [])" :key="item.id">
                  <td v-if="idx === 0" :rowspan="(parcel.items ?? []).length">{{ parcel.parcelNumber }}</td>
                  <td v-if="idx === 0" class="text-end" :rowspan="(parcel.items ?? []).length">{{ parcel.weightKg ? `${parcel.weightKg} kg` : '—' }}</td>
                  <td v-if="idx === 0" :rowspan="(parcel.items ?? []).length">{{ parcel.trackingNumber ?? '—' }}</td>
                  <td class="text-end">{{ item.quantity }}</td>
                  <td>{{ item.productCode ? `${item.productCode} - ${item.productName}` : `#${item.productId}` }}</td>
                </tr>
              </tbody>
            </v-table>
          </div>
        </v-expand-transition>
      </v-card>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import { useSalesOrderDetailView } from '@features/fulfillment/composables/useSalesOrderDetailView';

const vm = reactive(useSalesOrderDetailView());
const packingCollapsed = ref(false);

const showPackingSection = computed((): boolean => {
  const order = vm.order;
  if (!order) return false;
  return !!order.shipment || (order.parcels ?? []).length > 0;
});

const cardSpacing = computed((): string => (vm.layout.isCompact ? 'mb-2' : 'mb-4'));
const cardPadding = computed((): string => (vm.layout.isCompact ? 'pa-3' : 'pa-4'));

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
</script>

<style scoped>
.so-detail :deep(.so-section-title) {
  padding: 8px 12px;
  font-size: 0.9rem;
  font-weight: 500;
  display: flex;
  align-items: center;
  min-height: unset;
}
.so-detail :deep(.v-card-title) {
  min-height: unset;
}
</style>
