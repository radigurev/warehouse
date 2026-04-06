<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.warehouse">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.warehouse.name }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.warehouse.code }}</div>
        </div>
        <v-spacer />
        <StatusChip :active="vm.warehouse.isActive" class="mr-2" />
        <v-btn v-if="!vm.warehouse.isActive" color="success" variant="tonal" prepend-icon="mdi-warehouse" @click="showReactivateDialog = true">
          {{ vm.t('warehouses.reactivate') }}
        </v-btn>
        <v-btn v-else color="error" variant="tonal" prepend-icon="mdi-warehouse" @click="showDeactivateDialog = true">
          {{ vm.t('warehouses.deactivate') }}
        </v-btn>
      </div>

      <!-- Warehouse Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('warehouses.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('warehouses.form.address') }}</div>
              <div>{{ vm.warehouse.address || '\u2014' }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('warehouses.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.warehouse.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.warehouse.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('warehouses.form.notes') }}</div>
              <div class="text-body-2">{{ vm.warehouse.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Zones Section -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-select-group" class="mr-2" />
          {{ vm.t('warehouses.detail.zones') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showZoneForm = true">
            {{ vm.t('warehouses.detail.addZone') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.warehouse.zones.length === 0" class="text-medium-emphasis">
          {{ vm.t('warehouses.detail.noZones') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('zones.columns.code') }}</th>
              <th>{{ vm.t('zones.columns.name') }}</th>
              <th>{{ vm.t('zones.columns.description') }}</th>
              <th style="width: 120px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="zone in vm.warehouse.zones" :key="zone.id">
              <td>{{ zone.code }}</td>
              <td>{{ zone.name }}</td>
              <td>{{ zone.description || '\u2014' }}</td>
              <td>
                <v-btn icon="mdi-pencil" size="small" variant="text" color="primary" @click="openEditZone(zone)" />
                <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="openDeleteZone(zone)" />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>

      <!-- Storage Locations Section -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-map-marker-multiple" class="mr-2" />
          {{ vm.t('storageLocations.title') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showLocationForm = true">
            {{ vm.t('storageLocations.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.locations.length === 0" class="text-medium-emphasis">
          {{ vm.t('common.noData') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('storageLocations.columns.code') }}</th>
              <th>{{ vm.t('storageLocations.columns.name') }}</th>
              <th>{{ vm.t('storageLocations.columns.zone') }}</th>
              <th>{{ vm.t('storageLocations.columns.locationType') }}</th>
              <th>{{ vm.t('storageLocations.columns.capacity') }}</th>
              <th style="width: 120px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="loc in vm.locations" :key="loc.id">
              <td>{{ loc.code }}</td>
              <td>{{ loc.name }}</td>
              <td>{{ loc.zoneName || '\u2014' }}</td>
              <td>{{ loc.locationType }}</td>
              <td>{{ loc.capacity ?? '\u2014' }}</td>
              <td>
                <v-btn icon="mdi-pencil" size="small" variant="text" color="primary" @click="openEditLocation(loc)" />
                <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="openDeleteLocation(loc)" />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Zone Form Dialog -->
    <FormWrapper v-model="showZoneForm" max-width="450" :title="editingZone ? vm.t('zones.edit') : vm.t('zones.create')" icon="mdi-select-group">
      <v-card-text>
        <v-form ref="zoneFormRef" @submit.prevent="submitZone">
          <v-text-field v-model="zoneForm.code" :label="vm.t('zones.form.code')" :density="vm.layout.vuetifyDensity" :rules="[requiredRule, codeFormatRule]" :disabled="!!editingZone" />
          <v-text-field v-model="zoneForm.name" :label="vm.t('zones.form.name')" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
          <v-textarea v-model="zoneForm.description" :label="vm.t('zones.form.description')" :density="vm.layout.vuetifyDensity" rows="2" auto-grow />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showZoneForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitZone">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Location Form Dialog -->
    <FormWrapper v-model="showLocationForm" max-width="450" :title="editingLocation ? vm.t('storageLocations.edit') : vm.t('storageLocations.create')" icon="mdi-map-marker-plus">
      <v-card-text>
        <v-form ref="locationFormRef" @submit.prevent="submitLocation">
          <v-select
            v-model="locationForm.zoneId"
            :label="vm.t('storageLocations.form.zone')"
            :items="vm.warehouse?.zones ?? []"
            item-title="name"
            item-value="id"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule]"
            :disabled="!!editingLocation"
          />
          <v-text-field v-model="locationForm.code" :label="vm.t('storageLocations.form.code')" :density="vm.layout.vuetifyDensity" :rules="[requiredRule, codeFormatRule]" :disabled="!!editingLocation" />
          <v-text-field v-model="locationForm.name" :label="vm.t('storageLocations.form.name')" :density="vm.layout.vuetifyDensity" :rules="[requiredRule]" />
          <v-select
            v-model="locationForm.locationType"
            :label="vm.t('storageLocations.form.locationType')"
            :items="locationTypes"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule]"
          />
          <v-text-field v-model.number="locationForm.capacity" :label="vm.t('storageLocations.form.capacity')" type="number" :density="vm.layout.vuetifyDensity" :min="0" />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showLocationForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitLocation">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Delete Zone Confirmation -->
    <ConfirmDialog
      v-model="showDeleteZoneDialog"
      :title="vm.t('zones.delete')"
      :message="vm.t('zones.deleteConfirm', { name: deletingZone?.name })"
      :confirm-text="vm.t('common.delete')"
      color="error"
      icon="mdi-delete"
      :loading="deletingZoneLoading"
      @confirm="confirmDeleteZone"
    />

    <!-- Delete Location Confirmation -->
    <ConfirmDialog
      v-model="showDeleteLocationDialog"
      :title="vm.t('storageLocations.delete')"
      :message="vm.t('storageLocations.deleteConfirm', { name: deletingLocation?.name })"
      :confirm-text="vm.t('common.delete')"
      color="error"
      icon="mdi-delete"
      :loading="deletingLocationLoading"
      @confirm="confirmDeleteLocation"
    />

    <!-- Deactivate Confirmation -->
    <ConfirmDialog
      v-model="showDeactivateDialog"
      :title="vm.t('warehouses.deactivate')"
      :message="vm.t('warehouses.deactivateConfirm', { name: vm.warehouse?.name })"
      :confirm-text="vm.t('warehouses.deactivate')"
      color="error"
      icon="mdi-warehouse"
      :loading="deactivating"
      @confirm="handleDeactivate"
    />

    <!-- Reactivate Confirmation -->
    <ConfirmDialog
      v-model="showReactivateDialog"
      :title="vm.t('warehouses.reactivate')"
      :message="vm.t('warehouses.reactivateConfirm', { name: vm.warehouse?.name })"
      :confirm-text="vm.t('warehouses.reactivate')"
      color="success"
      icon="mdi-warehouse"
      :loading="reactivating"
      @confirm="handleReactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useWarehouseDetailView } from '@features/inventory/composables/useWarehouseDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import type { ZoneDto, StorageLocationDto } from '@features/inventory/types/inventory';

const notification = useNotificationStore();
const vm = reactive(useWarehouseDetailView());

const locationTypes = ['Row', 'Shelf', 'Bin', 'Bulk'];

// --- Deactivate / Reactivate ---
const showDeactivateDialog = ref(false);
const showReactivateDialog = ref(false);
const deactivating = ref(false);
const reactivating = ref(false);

async function handleDeactivate(): Promise<void> {
  deactivating.value = true;
  try {
    await vm.handleDeactivate();
    showDeactivateDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    deactivating.value = false;
  }
}

async function handleReactivate(): Promise<void> {
  reactivating.value = true;
  try {
    await vm.handleReactivate();
    showReactivateDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    reactivating.value = false;
  }
}

// --- Zone Form ---
const showZoneForm = ref(false);
const editingZone = ref<ZoneDto | null>(null);
const zoneFormRef = ref();

const zoneForm = reactive({
  code: '',
  name: '',
  description: '',
});

function openEditZone(zone: ZoneDto): void {
  editingZone.value = zone;
  zoneForm.code = zone.code;
  zoneForm.name = zone.name;
  zoneForm.description = zone.description ?? '';
  showZoneForm.value = true;
}

function resetZoneForm(): void {
  editingZone.value = null;
  zoneForm.code = '';
  zoneForm.name = '';
  zoneForm.description = '';
}

async function submitZone(): Promise<void> {
  const { valid } = await zoneFormRef.value.validate();
  if (!valid) return;
  try {
    if (editingZone.value) {
      await vm.handleUpdateZone(editingZone.value.id, {
        name: zoneForm.name,
        description: zoneForm.description || null,
      });
    } else {
      await vm.handleCreateZone({
        warehouseId: vm.warehouseId,
        code: zoneForm.code,
        name: zoneForm.name,
        description: zoneForm.description || null,
      });
    }
    showZoneForm.value = false;
    resetZoneForm();
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  }
}

// --- Delete Zone ---
const showDeleteZoneDialog = ref(false);
const deletingZone = ref<ZoneDto | null>(null);
const deletingZoneLoading = ref(false);

function openDeleteZone(zone: ZoneDto): void {
  deletingZone.value = zone;
  showDeleteZoneDialog.value = true;
}

async function confirmDeleteZone(): Promise<void> {
  if (!deletingZone.value) return;
  deletingZoneLoading.value = true;
  try {
    await vm.handleDeleteZone(deletingZone.value.id);
    showDeleteZoneDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    deletingZoneLoading.value = false;
  }
}

// --- Location Form ---
const showLocationForm = ref(false);
const editingLocation = ref<StorageLocationDto | null>(null);
const locationFormRef = ref();

const locationForm = reactive({
  zoneId: null as number | null,
  code: '',
  name: '',
  locationType: 'Shelf',
  capacity: null as number | null,
});

function openEditLocation(loc: StorageLocationDto): void {
  editingLocation.value = loc;
  locationForm.zoneId = loc.zoneId;
  locationForm.code = loc.code;
  locationForm.name = loc.name;
  locationForm.locationType = loc.locationType;
  locationForm.capacity = loc.capacity;
  showLocationForm.value = true;
}

function resetLocationForm(): void {
  editingLocation.value = null;
  locationForm.zoneId = null;
  locationForm.code = '';
  locationForm.name = '';
  locationForm.locationType = 'Shelf';
  locationForm.capacity = null;
}

async function submitLocation(): Promise<void> {
  const { valid } = await locationFormRef.value.validate();
  if (!valid) return;
  try {
    if (editingLocation.value) {
      await vm.handleUpdateLocation(editingLocation.value.id, {
        name: locationForm.name,
        locationType: locationForm.locationType,
        capacity: locationForm.capacity,
      });
    } else {
      await vm.handleCreateLocation({
        zoneId: locationForm.zoneId!,
        code: locationForm.code,
        name: locationForm.name,
        locationType: locationForm.locationType,
        capacity: locationForm.capacity,
      });
    }
    showLocationForm.value = false;
    resetLocationForm();
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  }
}

// --- Delete Location ---
const showDeleteLocationDialog = ref(false);
const deletingLocation = ref<StorageLocationDto | null>(null);
const deletingLocationLoading = ref(false);

function openDeleteLocation(loc: StorageLocationDto): void {
  deletingLocation.value = loc;
  showDeleteLocationDialog.value = true;
}

async function confirmDeleteLocation(): Promise<void> {
  if (!deletingLocation.value) return;
  deletingLocationLoading.value = true;
  try {
    await vm.handleDeleteLocation(deletingLocation.value.id);
    showDeleteLocationDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    deletingLocationLoading.value = false;
  }
}

// --- Validation rules ---
const requiredRule = (v: unknown) => !!v || vm.t('common.required');
const codeFormatRule = (v: string) => !v || /^[a-zA-Z0-9-]*$/.test(v) || vm.t('validation.codeFormat');
</script>
