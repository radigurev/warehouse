<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.shipment">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.shipment.shipmentNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.detail.forSo') }} {{ vm.shipment.salesOrderNumber }}</div>
        </div>
        <v-spacer />
        <v-chip :color="shipmentStatusColor(vm.shipment.status)" size="small" label class="mr-2">
          {{ vm.t(`shipments.status.${vm.shipment.status}`) }}
        </v-chip>
        <v-btn v-if="!isTerminalStatus(vm.shipment.status)" color="primary" variant="tonal" prepend-icon="mdi-update" @click="showStatusDialog = true">
          {{ vm.t('shipments.updateStatus') }}
        </v-btn>
      </div>

      <!-- Shipment Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('shipments.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.columns.salesOrderNumber') }}</div>
              <div>
                <a class="text-primary cursor-pointer" @click="vm.navigateToSalesOrder()">{{ vm.shipment.salesOrderNumber }}</a>
              </div>
            </v-col>
            <v-col v-if="vm.shipment.carrierName" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.columns.carrier') }}</div>
              <div>{{ vm.shipment.carrierName }}{{ vm.shipment.carrierServiceLevelName ? ' - ' + vm.shipment.carrierServiceLevelName : '' }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.columns.dispatchedAt') }}</div>
              <div>{{ vm.formatDate(vm.shipment.dispatchedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.shipment.trackingNumber" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.columns.trackingNumber') }}</div>
              <div>
                <a v-if="vm.shipment.trackingUrl" :href="vm.shipment.trackingUrl" target="_blank" class="text-primary">{{ vm.shipment.trackingNumber }}</a>
                <span v-else>{{ vm.shipment.trackingNumber }}</span>
              </div>
            </v-col>
            <v-col v-if="vm.shipment.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('shipments.detail.notes') }}</div>
              <div class="text-body-2">{{ vm.shipment.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Shipping Address Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-map-marker" class="mr-2" />
          {{ vm.t('shipments.detail.shippingAddress') }}
        </v-card-title>
        <v-card-text>
          <div>{{ vm.shipment.shippingStreetLine1 }}</div>
          <div v-if="vm.shipment.shippingStreetLine2">{{ vm.shipment.shippingStreetLine2 }}</div>
          <div>{{ vm.shipment.shippingCity }}{{ vm.shipment.shippingStateProvince ? ', ' + vm.shipment.shippingStateProvince : '' }} {{ vm.shipment.shippingPostalCode }}</div>
          <div>{{ vm.shipment.shippingCountryCode }}</div>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('shipments.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.shipment.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('shipments.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('shipments.lines.product') }}</th>
              <th class="text-end">{{ vm.t('shipments.lines.shippedQty') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.shipment.lines" :key="line.id">
              <td>{{ line.productCode }} - {{ line.productName }}</td>
              <td class="text-end">{{ line.shippedQuantity }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>

      <!-- Parcels Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-package-variant" class="mr-2" />
          {{ vm.t('shipments.detail.parcels') }}
        </v-card-title>
        <v-card-text v-if="vm.shipment.parcels.length === 0" class="text-medium-emphasis">
          {{ vm.t('shipments.detail.noParcels') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('shipments.parcels.parcelNumber') }}</th>
              <th class="text-end">{{ vm.t('shipments.parcels.weight') }}</th>
              <th>{{ vm.t('shipments.parcels.dimensions') }}</th>
              <th>{{ vm.t('shipments.parcels.trackingNumber') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="parcel in vm.shipment.parcels" :key="parcel.id">
              <td>{{ parcel.parcelNumber }}</td>
              <td class="text-end">{{ parcel.weightKg ? parcel.weightKg + ' kg' : '\u2014' }}</td>
              <td>{{ parcel.lengthCm ? `${parcel.lengthCm} x ${parcel.widthCm} x ${parcel.heightCm} cm` : '\u2014' }}</td>
              <td>{{ parcel.trackingNumber || '\u2014' }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>

      <!-- Tracking History Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-timeline" class="mr-2" />
          {{ vm.t('shipments.detail.trackingHistory') }}
        </v-card-title>
        <v-card-text v-if="vm.shipment.trackingHistory.length === 0" class="text-medium-emphasis">
          {{ vm.t('shipments.detail.noTrackingHistory') }}
        </v-card-text>
        <v-timeline v-else density="compact" side="end" class="pa-4">
          <v-timeline-item
            v-for="entry in vm.shipment.trackingHistory"
            :key="entry.id"
            :dot-color="shipmentStatusColor(entry.status)"
            size="small"
          >
            <div class="d-flex align-center">
              <v-chip :color="shipmentStatusColor(entry.status)" size="small" label class="mr-2">
                {{ vm.t(`shipments.status.${entry.status}`) }}
              </v-chip>
              <span class="text-caption text-medium-emphasis">{{ vm.formatDate(entry.updatedAtUtc) }}</span>
            </div>
            <div v-if="entry.notes" class="text-body-2 mt-1">{{ entry.notes }}</div>
          </v-timeline-item>
        </v-timeline>
      </v-card>
    </template>

    <!-- Update Status Dialog -->
    <ShipmentStatusDialog
      v-model="showStatusDialog"
      :current-status="vm.shipment?.status ?? 'Dispatched'"
      @updated="onStatusUpdated"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useShipmentDetailView } from '@features/fulfillment/composables/useShipmentDetailView';
import ShipmentStatusDialog from '@features/fulfillment/components/organisms/ShipmentStatusDialog.vue';

const vm = reactive(useShipmentDetailView());

const showStatusDialog = ref(false);

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

function isTerminalStatus(status: string): boolean {
  return status === 'Delivered' || status === 'Returned';
}

async function onStatusUpdated(): Promise<void> {
  showStatusDialog.value = false;
  await vm.loadShipment();
}
</script>
