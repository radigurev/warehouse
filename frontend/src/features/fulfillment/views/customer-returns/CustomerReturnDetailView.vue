<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.customerReturn">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.customerReturn.returnNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.customerReturn.customerName }}</div>
        </div>
        <v-spacer />
        <v-chip :color="returnStatusColor(vm.customerReturn.status)" size="small" label class="mr-2">
          {{ vm.t(`customerReturns.status.${vm.customerReturn.status}`) }}
        </v-chip>
        <v-btn v-if="vm.customerReturn.status === 'Draft'" color="success" variant="tonal" prepend-icon="mdi-check" class="mr-2" @click="showConfirmDialog = true">
          {{ vm.t('customerReturns.confirm') }}
        </v-btn>
        <v-btn v-if="vm.customerReturn.status === 'Confirmed'" color="primary" variant="tonal" prepend-icon="mdi-package-down" class="mr-2" @click="showReceiveDialog = true">
          {{ vm.t('customerReturns.receive') }}
        </v-btn>
        <v-btn v-if="vm.customerReturn.status === 'Received'" color="blue-grey" variant="tonal" prepend-icon="mdi-lock" class="mr-2" @click="showCloseDialog = true">
          {{ vm.t('customerReturns.close') }}
        </v-btn>
        <v-btn v-if="vm.customerReturn.status === 'Draft' || vm.customerReturn.status === 'Confirmed'" color="error" variant="tonal" prepend-icon="mdi-close" @click="showCancelDialog = true">
          {{ vm.t('customerReturns.cancel') }}
        </v-btn>
      </div>

      <!-- Return Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('customerReturns.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.columns.customerName') }}</div>
              <div>{{ vm.customerReturn.customerName }}</div>
            </v-col>
            <v-col v-if="vm.customerReturn.salesOrderNumber" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.columns.salesOrderNumber') }}</div>
              <div>
                <a class="text-primary cursor-pointer" @click="vm.navigateToSalesOrder()">{{ vm.customerReturn.salesOrderNumber }}</a>
              </div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.columns.reason') }}</div>
              <div>{{ vm.customerReturn.reason }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.customerReturn.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.customerReturn.confirmedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.detail.confirmedAt') }}</div>
              <div>{{ vm.formatDate(vm.customerReturn.confirmedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.customerReturn.receivedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.detail.receivedAt') }}</div>
              <div>{{ vm.formatDate(vm.customerReturn.receivedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.customerReturn.closedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.detail.closedAt') }}</div>
              <div>{{ vm.formatDate(vm.customerReturn.closedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.customerReturn.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('customerReturns.form.notes') }}</div>
              <div class="text-body-2">{{ vm.customerReturn.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('customerReturns.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.customerReturn.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('customerReturns.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('customerReturns.lines.product') }}</th>
              <th>{{ vm.t('customerReturns.lines.warehouse') }}</th>
              <th>{{ vm.t('customerReturns.lines.location') }}</th>
              <th class="text-end">{{ vm.t('customerReturns.lines.quantity') }}</th>
              <th>{{ vm.t('customerReturns.lines.notes') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.customerReturn.lines" :key="line.id">
              <td>{{ line.productCode }} - {{ line.productName }}</td>
              <td>{{ line.warehouseName }}</td>
              <td>{{ line.locationCode || '\u2014' }}</td>
              <td class="text-end">{{ line.quantity }}</td>
              <td>{{ line.notes || '\u2014' }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Confirm Dialog -->
    <ConfirmDialog
      v-model="showConfirmDialog"
      :title="vm.t('customerReturns.confirm')"
      :message="vm.t('customerReturns.confirmMessage', { returnNumber: vm.customerReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.confirm')"
      color="success"
      icon="mdi-check"
      :loading="confirming"
      @confirm="handleConfirm"
    />

    <!-- Receive Dialog -->
    <ConfirmDialog
      v-model="showReceiveDialog"
      :title="vm.t('customerReturns.receive')"
      :message="vm.t('customerReturns.receiveMessage', { returnNumber: vm.customerReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.receive')"
      color="primary"
      icon="mdi-package-down"
      :loading="receiving"
      @confirm="handleReceive"
    />

    <!-- Close Dialog -->
    <ConfirmDialog
      v-model="showCloseDialog"
      :title="vm.t('customerReturns.close')"
      :message="vm.t('customerReturns.closeMessage', { returnNumber: vm.customerReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.close')"
      color="blue-grey"
      icon="mdi-lock"
      :loading="closing"
      @confirm="handleClose"
    />

    <!-- Cancel Dialog -->
    <ConfirmDialog
      v-model="showCancelDialog"
      :title="vm.t('customerReturns.cancel')"
      :message="vm.t('customerReturns.cancelMessage', { returnNumber: vm.customerReturn?.returnNumber })"
      :confirm-text="vm.t('customerReturns.cancel')"
      color="error"
      icon="mdi-close"
      :loading="cancelling"
      @confirm="handleCancel"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useCustomerReturnDetailView } from '@features/fulfillment/composables/useCustomerReturnDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const notification = useNotificationStore();
const vm = reactive(useCustomerReturnDetailView());

const showConfirmDialog = ref(false);
const showReceiveDialog = ref(false);
const showCloseDialog = ref(false);
const showCancelDialog = ref(false);
const confirming = ref(false);
const receiving = ref(false);
const closing = ref(false);
const cancelling = ref(false);

function returnStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    Received: 'amber',
    Closed: 'green',
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

async function handleReceive(): Promise<void> {
  receiving.value = true;
  try {
    await vm.handleReceive();
    showReceiveDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    receiving.value = false;
  }
}

async function handleClose(): Promise<void> {
  closing.value = true;
  try {
    await vm.handleClose();
    showCloseDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    closing.value = false;
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
