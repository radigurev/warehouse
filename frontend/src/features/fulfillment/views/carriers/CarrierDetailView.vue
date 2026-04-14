<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.carrier">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.carrier.name }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.carrier.code }}</div>
        </div>
        <v-spacer />
        <StatusChip :active="vm.carrier.isActive" class="mr-2" />
        <v-btn color="primary" variant="tonal" prepend-icon="mdi-pencil" class="mr-2" @click="handleEdit">
          {{ vm.t('common.edit') }}
        </v-btn>
        <v-btn v-if="vm.carrier.isActive" color="error" variant="tonal" prepend-icon="mdi-toggle-switch-off" @click="showDeactivateDialog = true">
          {{ vm.t('carriers.deactivate') }}
        </v-btn>
      </div>

      <!-- Carrier Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('carriers.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col v-if="vm.carrier.contactPhone" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('carriers.form.contactPhone') }}</div>
              <div>{{ vm.carrier.contactPhone }}</div>
            </v-col>
            <v-col v-if="vm.carrier.contactEmail" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('carriers.form.contactEmail') }}</div>
              <div>{{ vm.carrier.contactEmail }}</div>
            </v-col>
            <v-col v-if="vm.carrier.websiteUrl" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('carriers.form.websiteUrl') }}</div>
              <div><a :href="vm.carrier.websiteUrl" target="_blank" class="text-primary">{{ vm.carrier.websiteUrl }}</a></div>
            </v-col>
            <v-col v-if="vm.carrier.trackingUrlTemplate" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('carriers.form.trackingUrlTemplate') }}</div>
              <div class="text-body-2">{{ vm.carrier.trackingUrlTemplate }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('carriers.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.carrier.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.carrier.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('carriers.form.notes') }}</div>
              <div class="text-body-2">{{ vm.carrier.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Service Levels Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-speedometer" class="mr-2" />
          {{ vm.t('carriers.detail.serviceLevels') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="openAddServiceLevel">
            {{ vm.t('carriers.detail.addServiceLevel') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.carrier.serviceLevels.length === 0" class="text-medium-emphasis">
          {{ vm.t('carriers.detail.noServiceLevels') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('carriers.serviceLevel.code') }}</th>
              <th>{{ vm.t('carriers.serviceLevel.name') }}</th>
              <th class="text-end">{{ vm.t('carriers.serviceLevel.estimatedDeliveryDays') }}</th>
              <th class="text-end">{{ vm.t('carriers.serviceLevel.baseRate') }}</th>
              <th class="text-end">{{ vm.t('carriers.serviceLevel.perKgRate') }}</th>
              <th style="width: 120px">{{ vm.t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="sl in vm.carrier.serviceLevels" :key="sl.id">
              <td>{{ sl.code }}</td>
              <td>{{ sl.name }}</td>
              <td class="text-end">{{ sl.estimatedDeliveryDays ?? '\u2014' }}</td>
              <td class="text-end">{{ sl.baseRate != null ? sl.baseRate.toFixed(2) : '\u2014' }}</td>
              <td class="text-end">{{ sl.perKgRate != null ? sl.perKgRate.toFixed(2) : '\u2014' }}</td>
              <td>
                <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="openEditServiceLevel(sl)" />
                <ActionChip :label="vm.t('common.delete')" icon="mdi-delete" color="error" :compact="vm.layout.isCompact" @click="openDeleteServiceLevel(sl)" />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Deactivate Dialog -->
    <ConfirmDialog
      v-model="showDeactivateDialog"
      :title="vm.t('carriers.deactivate')"
      :message="vm.t('carriers.deactivateConfirm', { name: vm.carrier?.name })"
      :confirm-text="vm.t('carriers.deactivate')"
      color="error"
      icon="mdi-toggle-switch-off"
      :loading="deactivating"
      @confirm="handleDeactivate"
    />

    <!-- Delete Service Level Dialog -->
    <ConfirmDialog
      v-model="showDeleteServiceLevelDialog"
      :title="vm.t('carriers.detail.deleteServiceLevel')"
      :message="vm.t('carriers.detail.deleteServiceLevelConfirm', { name: deletingServiceLevel?.name })"
      :confirm-text="vm.t('common.delete')"
      color="error"
      icon="mdi-delete"
      :loading="deletingLevel"
      @confirm="handleDeleteServiceLevel"
    />

    <!-- Service Level Form Dialog -->
    <ServiceLevelFormDialog
      v-model="showServiceLevelDialog"
      :carrier-id="vm.carrierId"
      :service-level="editingServiceLevel"
      @saved="onServiceLevelSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { useCarrierDetailView } from '@features/fulfillment/composables/useCarrierDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import ServiceLevelFormDialog from '@features/fulfillment/components/organisms/ServiceLevelFormDialog.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { CarrierServiceLevelDto } from '@features/fulfillment/types/fulfillment';

const router = useRouter();
const notification = useNotificationStore();
const vm = reactive(useCarrierDetailView());

const showDeactivateDialog = ref(false);
const deactivating = ref(false);

const showServiceLevelDialog = ref(false);
const editingServiceLevel = ref<CarrierServiceLevelDto | null>(null);

const showDeleteServiceLevelDialog = ref(false);
const deletingServiceLevel = ref<CarrierServiceLevelDto | null>(null);
const deletingLevel = ref(false);

function handleEdit(): void {
  router.push({ name: 'carrier-edit', params: { id: vm.carrierId } });
}

function openAddServiceLevel(): void {
  editingServiceLevel.value = null;
  showServiceLevelDialog.value = true;
}

function openEditServiceLevel(sl: CarrierServiceLevelDto): void {
  editingServiceLevel.value = sl;
  showServiceLevelDialog.value = true;
}

function openDeleteServiceLevel(sl: CarrierServiceLevelDto): void {
  deletingServiceLevel.value = sl;
  showDeleteServiceLevelDialog.value = true;
}

async function handleDeactivate(): Promise<void> {
  deactivating.value = true;
  try {
    await vm.handleDeactivate();
    showDeactivateDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    deactivating.value = false;
  }
}

async function handleDeleteServiceLevel(): Promise<void> {
  if (!deletingServiceLevel.value) return;
  deletingLevel.value = true;
  try {
    await vm.handleDeleteServiceLevel(deletingServiceLevel.value.id);
    showDeleteServiceLevelDialog.value = false;
    deletingServiceLevel.value = null;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    deletingLevel.value = false;
  }
}

async function onServiceLevelSaved(): Promise<void> {
  showServiceLevelDialog.value = false;
  editingServiceLevel.value = null;
  await vm.loadCarrier();
}
</script>
