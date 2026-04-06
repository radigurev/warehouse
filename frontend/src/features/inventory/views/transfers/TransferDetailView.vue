<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.transfer">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.t('transfers.title') }} #{{ vm.transfer.id }}</div>
        </div>
        <v-spacer />
        <v-chip :color="vm.statusColor(vm.transfer.status)" size="small" variant="flat" class="mr-2">
          {{ vm.t(`transfers.statuses.${vm.transfer.status}`) }}
        </v-chip>
        <v-btn v-if="vm.canComplete" color="success" variant="tonal" prepend-icon="mdi-check" class="mr-2" @click="showCompleteDialog = true">
          {{ vm.t('transfers.complete') }}
        </v-btn>
        <v-btn v-if="vm.canCancel" color="error" variant="tonal" prepend-icon="mdi-close" @click="showCancelDialog = true">
          {{ vm.t('transfers.cancel') }}
        </v-btn>
      </div>

      <!-- Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('transfers.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('transfers.columns.sourceWarehouse') }}</div>
              <div>{{ vm.transfer.sourceWarehouseName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('transfers.columns.destinationWarehouse') }}</div>
              <div>{{ vm.transfer.destinationWarehouseName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('transfers.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.transfer.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.transfer.completedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('transfers.complete') }}</div>
              <div>{{ vm.formatDate(vm.transfer.completedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.transfer.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('transfers.form.notes') }}</div>
              <div class="text-body-2">{{ vm.transfer.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('transfers.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.transfer.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('transfers.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('transfers.lines.product') }}</th>
              <th class="text-end">{{ vm.t('transfers.lines.quantity') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.transfer.lines" :key="line.id">
              <td>{{ line.productName }}</td>
              <td class="text-end">{{ line.quantity }}</td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Complete Confirmation -->
    <ConfirmDialog
      v-model="showCompleteDialog"
      :title="vm.t('transfers.complete')"
      :message="vm.t('transfers.completeConfirm')"
      :confirm-text="vm.t('transfers.complete')"
      color="success"
      icon="mdi-check"
      :loading="completing"
      @confirm="handleComplete"
    />

    <!-- Cancel Confirmation -->
    <ConfirmDialog
      v-model="showCancelDialog"
      :title="vm.t('transfers.cancel')"
      :message="vm.t('transfers.cancelConfirm')"
      :confirm-text="vm.t('transfers.cancel')"
      color="error"
      icon="mdi-close"
      :loading="cancelling"
      @confirm="handleCancel"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useTransferDetailView } from '@features/inventory/composables/useTransferDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const notification = useNotificationStore();
const vm = reactive(useTransferDetailView());

const showCompleteDialog = ref(false);
const showCancelDialog = ref(false);
const completing = ref(false);
const cancelling = ref(false);

async function handleComplete(): Promise<void> {
  completing.value = true;
  try {
    await vm.handleComplete();
    showCompleteDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    completing.value = false;
  }
}

async function handleCancel(): Promise<void> {
  cancelling.value = true;
  try {
    await vm.handleCancel();
    showCancelDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    cancelling.value = false;
  }
}
</script>
