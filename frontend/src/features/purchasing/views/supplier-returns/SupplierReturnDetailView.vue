<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.supplierReturn">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.supplierReturn.returnNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.supplierReturn.supplierName }}</div>
        </div>
        <v-spacer />
        <v-chip :color="returnStatusColor(vm.supplierReturn.status)" size="small" label class="mr-2">
          {{ vm.t(`supplierReturns.status.${vm.supplierReturn.status}`) }}
        </v-chip>
        <v-btn v-if="vm.supplierReturn.status === 'Draft'" color="success" variant="tonal" prepend-icon="mdi-check" class="mr-2" @click="showConfirmDialog = true">
          {{ vm.t('supplierReturns.confirm') }}
        </v-btn>
        <v-btn v-if="vm.supplierReturn.status === 'Draft'" color="error" variant="tonal" prepend-icon="mdi-close" @click="showCancelDialog = true">
          {{ vm.t('supplierReturns.cancel') }}
        </v-btn>
      </div>

      <!-- Return Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('supplierReturns.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('supplierReturns.columns.supplierName') }}</div>
              <div>{{ vm.supplierReturn.supplierName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('supplierReturns.columns.reason') }}</div>
              <div>{{ vm.supplierReturn.reason }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('supplierReturns.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.supplierReturn.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.supplierReturn.confirmedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('supplierReturns.detail.confirmedAt') }}</div>
              <div>{{ vm.formatDate(vm.supplierReturn.confirmedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.supplierReturn.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('supplierReturns.form.notes') }}</div>
              <div class="text-body-2">{{ vm.supplierReturn.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('supplierReturns.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.supplierReturn.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('supplierReturns.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('supplierReturns.lines.product') }}</th>
              <th>{{ vm.t('supplierReturns.lines.warehouse') }}</th>
              <th>{{ vm.t('supplierReturns.lines.location') }}</th>
              <th class="text-end">{{ vm.t('supplierReturns.lines.quantity') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.supplierReturn.lines" :key="line.id">
              <td>{{ line.productId }}</td>
              <td>{{ line.warehouseId }}</td>
              <td>{{ line.locationId || '\u2014' }}</td>
              <td class="text-end">{{ line.quantity }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Confirm Dialog -->
    <ConfirmDialog
      v-model="showConfirmDialog"
      :title="vm.t('supplierReturns.confirm')"
      :message="vm.t('supplierReturns.confirmMessage', { returnNumber: vm.supplierReturn?.returnNumber })"
      :confirm-text="vm.t('supplierReturns.confirm')"
      color="success"
      icon="mdi-check"
      :loading="confirming"
      @confirm="handleConfirm"
    />

    <!-- Cancel Dialog -->
    <ConfirmDialog
      v-model="showCancelDialog"
      :title="vm.t('supplierReturns.cancel')"
      :message="vm.t('supplierReturns.cancelMessage', { returnNumber: vm.supplierReturn?.returnNumber })"
      :confirm-text="vm.t('supplierReturns.cancel')"
      color="error"
      icon="mdi-close"
      :loading="cancelling"
      @confirm="handleCancel"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useSupplierReturnDetailView } from '@features/purchasing/composables/useSupplierReturnDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const notification = useNotificationStore();
const vm = reactive(useSupplierReturnDetailView());

const showConfirmDialog = ref(false);
const showCancelDialog = ref(false);
const confirming = ref(false);
const cancelling = ref(false);

function returnStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
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
</script>
