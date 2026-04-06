<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.session">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.session.name }}</div>
        </div>
        <v-spacer />
        <v-chip :color="vm.statusColor(vm.session.status)" size="small" variant="flat" class="mr-2">
          {{ vm.translateStatus(vm.session.status) }}
        </v-chip>
        <v-btn v-if="vm.isDraft" color="info" variant="tonal" prepend-icon="mdi-play" class="mr-2" @click="showStartDialog = true">
          {{ vm.t('stocktake.start') }}
        </v-btn>
        <v-btn v-if="vm.isInProgress" color="success" variant="tonal" prepend-icon="mdi-check" class="mr-2" @click="showCompleteDialog = true">
          {{ vm.t('stocktake.complete') }}
        </v-btn>
        <v-btn v-if="vm.isInProgress" color="error" variant="tonal" prepend-icon="mdi-close" @click="showCancelDialog = true">
          {{ vm.t('stocktake.cancelSession') }}
        </v-btn>
        <v-btn v-if="vm.isCompleted" color="info" variant="tonal" prepend-icon="mdi-chart-bar" class="mr-2" @click="vm.handleViewVarianceReport()">
          {{ vm.t('stocktake.varianceReport') }}
        </v-btn>
        <v-btn v-if="vm.isCompleted" color="primary" variant="tonal" prepend-icon="mdi-pencil-ruler" @click="vm.handleCreateAdjustment()">
          {{ vm.t('stocktake.createAdjustment') }}
        </v-btn>
      </div>

      <!-- Session Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('stocktake.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('stocktake.columns.warehouse') }}</div>
              <div>{{ vm.session.warehouseName }}</div>
            </v-col>
            <v-col v-if="vm.session.zoneName" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('stocktake.columns.zone') }}</div>
              <div>{{ vm.session.zoneName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('stocktake.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.session.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.session.startedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('stocktake.start') }}</div>
              <div>{{ vm.formatDate(vm.session.startedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.session.completedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('stocktake.complete') }}</div>
              <div>{{ vm.formatDate(vm.session.completedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.session.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('stocktake.form.notes') }}</div>
              <div class="text-body-2">{{ vm.session.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Counts Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-counter" class="mr-2" />
          {{ vm.t('stocktake.detail.counts') }}
          <v-spacer />
          <v-btn v-if="vm.isInProgress" size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showCountForm = true">
            {{ vm.t('stocktake.counts.addCount') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.session.counts.length === 0" class="text-medium-emphasis">
          {{ vm.t('stocktake.detail.noCounts') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('stocktake.counts.product') }}</th>
              <th>{{ vm.t('stocktake.counts.location') }}</th>
              <th class="text-end">{{ vm.t('stocktake.counts.expected') }}</th>
              <th class="text-end">{{ vm.t('stocktake.counts.actual') }}</th>
              <th class="text-end">{{ vm.t('stocktake.counts.variance') }}</th>
              <th v-if="vm.isInProgress" style="width: 120px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="count in vm.session.counts" :key="count.id">
              <td>{{ count.productName }}</td>
              <td>{{ count.locationName || '\u2014' }}</td>
              <td class="text-end">{{ count.expectedQuantity }}</td>
              <td class="text-end">{{ count.actualQuantity }}</td>
              <td class="text-end">
                <span :class="vm.varianceColor(count.variance) ? `text-${vm.varianceColor(count.variance)}` : ''">
                  {{ count.variance > 0 ? '+' : '' }}{{ count.variance }}
                </span>
              </td>
              <td v-if="vm.isInProgress">
                <v-btn icon="mdi-pencil" size="small" variant="text" color="primary" @click="openEditCount(count)" />
                <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="openDeleteCount(count)" />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Add Count Dialog -->
    <FormWrapper v-model="showCountForm" max-width="450" :title="editingCount ? vm.t('common.edit') : vm.t('stocktake.counts.addCount')" icon="mdi-counter">
      <v-card-text>
        <v-form ref="countFormRef" @submit.prevent="submitCount">
          <v-autocomplete
            v-if="!editingCount"
            v-model="countForm.productId"
            :label="vm.t('stocktake.counts.product')"
            :items="products"
            item-title="name"
            item-value="id"
            :density="vm.layout.vuetifyDensity"
            :loading="productsLoading"
            :rules="[requiredRule]"
          />
          <v-autocomplete
            v-if="!editingCount"
            v-model="countForm.locationId"
            :label="vm.t('stocktake.counts.location')"
            :items="locations"
            item-title="name"
            item-value="id"
            :density="vm.layout.vuetifyDensity"
            :loading="locationsLoading"
            clearable
          />
          <v-text-field
            v-model.number="countForm.countedQuantity"
            :label="vm.t('stocktake.counts.actual')"
            type="number"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule, positiveOrZeroRule]"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showCountForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" :loading="submittingCount" @click="submitCount">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Delete Count Confirmation -->
    <ConfirmDialog
      v-model="showDeleteCountDialog"
      :title="vm.t('common.delete')"
      :message="deletingCount ? vm.t('stocktake.counts.product') + ': ' + deletingCount.productName : ''"
      :confirm-text="vm.t('common.delete')"
      color="error"
      icon="mdi-delete"
      :loading="deletingCountLoading"
      @confirm="confirmDeleteCount"
    />

    <!-- Start Confirmation -->
    <ConfirmDialog
      v-model="showStartDialog"
      :title="vm.t('stocktake.start')"
      :message="vm.t('stocktake.startConfirm')"
      :confirm-text="vm.t('stocktake.start')"
      color="info"
      icon="mdi-play"
      :loading="starting"
      @confirm="handleStart"
    />

    <!-- Complete Confirmation -->
    <ConfirmDialog
      v-model="showCompleteDialog"
      :title="vm.t('stocktake.complete')"
      :message="vm.t('stocktake.completeConfirm')"
      :confirm-text="vm.t('stocktake.complete')"
      color="success"
      icon="mdi-check"
      :loading="completing"
      @confirm="handleComplete"
    />

    <!-- Cancel Confirmation -->
    <ConfirmDialog
      v-model="showCancelDialog"
      :title="vm.t('stocktake.cancelSession')"
      :message="vm.t('stocktake.cancelConfirm')"
      :confirm-text="vm.t('stocktake.cancelSession')"
      color="error"
      icon="mdi-close"
      :loading="cancelling"
      @confirm="handleCancel"
    />

    <!-- Variance Report Dialog -->
    <FormWrapper v-model="vm.showVarianceReport" max-width="700" :title="vm.t('stocktake.varianceReport')" icon="mdi-chart-bar">
      <v-card-text>
        <v-table :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('stocktake.counts.product') }}</th>
              <th>{{ vm.t('stocktake.counts.location') }}</th>
              <th class="text-end">{{ vm.t('stocktake.counts.expected') }}</th>
              <th class="text-end">{{ vm.t('stocktake.counts.actual') }}</th>
              <th class="text-end">{{ vm.t('stocktake.counts.variance') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="entry in vm.varianceReport" :key="entry.id">
              <td>{{ entry.productName }}</td>
              <td>{{ entry.locationName || '\u2014' }}</td>
              <td class="text-end">{{ entry.expectedQuantity }}</td>
              <td class="text-end">{{ entry.actualQuantity }}</td>
              <td class="text-end">
                <span :class="vm.varianceColor(entry.variance) ? `text-${vm.varianceColor(entry.variance)}` : ''">
                  {{ entry.variance > 0 ? '+' : '' }}{{ entry.variance }}
                </span>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="vm.showVarianceReport = false">{{ vm.t('common.close') }}</v-btn>
      </v-card-actions>
    </FormWrapper>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useStocktakeDetailView } from '@features/inventory/composables/useStocktakeDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import { searchProducts } from '@features/inventory/api/products';
import { searchLocations } from '@features/inventory/api/storage-locations';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import type { ProductDto, StorageLocationDto, StocktakeCountDto } from '@features/inventory/types/inventory';

const notification = useNotificationStore();
const vm = reactive(useStocktakeDetailView());

// --- Dropdown data ---
const products = ref<ProductDto[]>([]);
const productsLoading = ref(false);
const locations = ref<StorageLocationDto[]>([]);
const locationsLoading = ref(false);

onMounted(async () => {
  productsLoading.value = true;
  locationsLoading.value = true;
  try {
    const [prodRes, locRes] = await Promise.all([
      searchProducts({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 1000 }),
      searchLocations({ page: 1, pageSize: 1000 }),
    ]);
    products.value = prodRes.items;
    locations.value = locRes.items;
  } catch {
    // dropdowns empty
  } finally {
    productsLoading.value = false;
    locationsLoading.value = false;
  }
});

// --- Status action dialogs ---
const showStartDialog = ref(false);
const showCompleteDialog = ref(false);
const showCancelDialog = ref(false);
const starting = ref(false);
const completing = ref(false);
const cancelling = ref(false);

async function handleStart(): Promise<void> {
  starting.value = true;
  try {
    await vm.handleStart();
    showStartDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    starting.value = false;
  }
}

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

// --- Count Form ---
const showCountForm = ref(false);
const editingCount = ref<StocktakeCountDto | null>(null);
const countFormRef = ref();
const submittingCount = ref(false);

const countForm = reactive({
  productId: null as number | null,
  locationId: null as number | null,
  countedQuantity: null as number | null,
});

const requiredRule = (v: unknown) => (v !== null && v !== undefined && v !== '') || vm.t('common.required');
const positiveOrZeroRule = (v: number | null) => (v !== null && v >= 0) || vm.t('common.required');

function openEditCount(count: StocktakeCountDto): void {
  editingCount.value = count;
  countForm.productId = count.productId;
  countForm.locationId = count.locationId;
  countForm.countedQuantity = count.actualQuantity;
  showCountForm.value = true;
}

function resetCountForm(): void {
  editingCount.value = null;
  countForm.productId = null;
  countForm.locationId = null;
  countForm.countedQuantity = null;
}

async function submitCount(): Promise<void> {
  const { valid } = await countFormRef.value.validate();
  if (!valid) return;
  submittingCount.value = true;
  try {
    if (editingCount.value) {
      await vm.handleUpdateCount(editingCount.value.id, {
        countedQuantity: countForm.countedQuantity!,
      });
    } else {
      await vm.handleAddCount({
        productId: countForm.productId!,
        locationId: countForm.locationId ?? undefined,
        countedQuantity: countForm.countedQuantity!,
      });
    }
    showCountForm.value = false;
    resetCountForm();
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    submittingCount.value = false;
  }
}

// --- Delete Count ---
const showDeleteCountDialog = ref(false);
const deletingCount = ref<StocktakeCountDto | null>(null);
const deletingCountLoading = ref(false);

function openDeleteCount(count: StocktakeCountDto): void {
  deletingCount.value = count;
  showDeleteCountDialog.value = true;
}

async function confirmDeleteCount(): Promise<void> {
  if (!deletingCount.value) return;
  deletingCountLoading.value = true;
  try {
    await vm.handleDeleteCount(deletingCount.value.id);
    showDeleteCountDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    deletingCountLoading.value = false;
  }
}
</script>
