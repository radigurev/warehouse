<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.product">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.product.name }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.product.code }}</div>
        </div>
        <v-spacer />
        <StatusChip :active="vm.product.isActive" class="mr-2" />
        <v-btn v-if="!vm.product.isActive" color="success" variant="tonal" prepend-icon="mdi-package-variant-closed-check" @click="showReactivateDialog = true">
          {{ vm.t('products.reactivate') }}
        </v-btn>
        <v-btn v-else color="error" variant="tonal" prepend-icon="mdi-package-variant-closed-remove" @click="showDeactivateDialog = true">
          {{ vm.t('products.deactivate') }}
        </v-btn>
      </div>

      <!-- Product Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('products.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col v-if="vm.product.sku" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('products.form.sku') }}</div>
              <div>{{ vm.product.sku }}</div>
            </v-col>
            <v-col v-if="vm.product.barcode" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('products.form.barcode') }}</div>
              <div>{{ vm.product.barcode }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('products.form.category') }}</div>
              <div>{{ vm.product.categoryName || '\u2014' }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('products.form.unitOfMeasure') }}</div>
              <div>{{ vm.product.unitOfMeasureName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('products.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.product.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.product.description" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('products.form.description') }}</div>
              <div class="text-body-2">{{ vm.product.description }}</div>
            </v-col>
            <v-col v-if="vm.product.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('products.form.notes') }}</div>
              <div class="text-body-2">{{ vm.product.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- BOM Section -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-sitemap" class="mr-2" />
          {{ vm.t('products.detail.bom') }}
          <v-spacer />
          <v-btn v-if="!vm.bom" size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showBomCreateForm = true">
            {{ vm.t('products.detail.addBom') }}
          </v-btn>
          <v-btn v-else size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showBomLineForm = true">
            {{ vm.t('common.create') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="!vm.bom" class="text-medium-emphasis">
          {{ vm.t('products.detail.noBom') }}
        </v-card-text>
        <template v-else>
          <v-data-table
            :headers="bomHeaders"
            :items="vm.bom.lines"
            :density="vm.layout.vuetifyDensity"
            :items-per-page="-1"
            hide-default-footer
          >
            <template #item.actions="{ item }">
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="handleRemoveBomLine(item.id)" />
            </template>
          </v-data-table>
        </template>
      </v-card>

      <!-- Accessories Section -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-puzzle" class="mr-2" />
          {{ vm.t('products.detail.accessories') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showAccessoryForm = true">
            {{ vm.t('products.detail.addAccessory') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.accessories.length === 0" class="text-medium-emphasis">
          {{ vm.t('products.detail.noAccessories') }}
        </v-card-text>
        <v-list v-else :density="vm.layout.vuetifyDensity">
          <v-list-item v-for="acc in vm.accessories" :key="acc.id">
            <v-list-item-title>{{ acc.accessoryProductName }}</v-list-item-title>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="handleDeleteAccessory(acc.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>

      <!-- Substitutes Section -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium d-flex align-center">
          <v-icon icon="mdi-swap-horizontal" class="mr-2" />
          {{ vm.t('products.detail.substitutes') }}
          <v-spacer />
          <v-btn size="small" color="primary" variant="tonal" prepend-icon="mdi-plus" @click="showSubstituteForm = true">
            {{ vm.t('products.detail.addSubstitute') }}
          </v-btn>
        </v-card-title>
        <v-card-text v-if="vm.substitutes.length === 0" class="text-medium-emphasis">
          {{ vm.t('products.detail.noSubstitutes') }}
        </v-card-text>
        <v-list v-else :density="vm.layout.vuetifyDensity">
          <v-list-item v-for="sub in vm.substitutes" :key="sub.id">
            <v-list-item-title>{{ sub.substituteProductName }}</v-list-item-title>
            <template #append>
              <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="handleDeleteSubstitute(sub.id)" />
            </template>
          </v-list-item>
        </v-list>
      </v-card>
    </template>

    <!-- Create BOM Dialog -->
    <FormWrapper v-model="showBomCreateForm" max-width="500" :title="vm.t('products.detail.addBom')" icon="mdi-sitemap">
      <v-card-text>
        <v-form ref="bomCreateFormRef" @submit.prevent="submitCreateBom">
          <v-text-field v-model="bomCreateForm.name" label="BOM Name" :density="vm.layout.vuetifyDensity" />
          <v-autocomplete
            v-model="bomCreateForm.childProductId"
            :label="vm.t('products.form.name')"
            :items="availableProducts"
            item-title="name"
            item-value="id"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredSelectRule]"
          />
          <v-text-field
            v-model.number="bomCreateForm.quantity"
            label="Quantity"
            type="number"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule, positiveNumberRule]"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showBomCreateForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitCreateBom">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Add BOM Line Dialog -->
    <FormWrapper v-model="showBomLineForm" max-width="500" :title="vm.t('common.create')" icon="mdi-plus">
      <v-card-text>
        <v-form ref="bomLineFormRef" @submit.prevent="submitAddBomLine">
          <v-autocomplete
            v-model="bomLineForm.childProductId"
            :label="vm.t('products.form.name')"
            :items="availableProducts"
            item-title="name"
            item-value="id"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredSelectRule]"
          />
          <v-text-field
            v-model.number="bomLineForm.quantity"
            label="Quantity"
            type="number"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule, positiveNumberRule]"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showBomLineForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitAddBomLine">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Add Accessory Dialog -->
    <FormWrapper v-model="showAccessoryForm" max-width="500" :title="vm.t('products.detail.addAccessory')" icon="mdi-puzzle">
      <v-card-text>
        <v-form ref="accessoryFormRef" @submit.prevent="submitAddAccessory">
          <v-autocomplete
            v-model="accessoryForm.accessoryProductId"
            :label="vm.t('products.form.name')"
            :items="availableProducts"
            item-title="name"
            item-value="id"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredSelectRule]"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showAccessoryForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitAddAccessory">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Add Substitute Dialog -->
    <FormWrapper v-model="showSubstituteForm" max-width="500" :title="vm.t('products.detail.addSubstitute')" icon="mdi-swap-horizontal">
      <v-card-text>
        <v-form ref="substituteFormRef" @submit.prevent="submitAddSubstitute">
          <v-autocomplete
            v-model="substituteForm.substituteProductId"
            :label="vm.t('products.form.name')"
            :items="availableProducts"
            item-title="name"
            item-value="id"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredSelectRule]"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showSubstituteForm = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" @click="submitAddSubstitute">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Deactivate Confirmation -->
    <ConfirmDialog
      v-model="showDeactivateDialog"
      :title="vm.t('products.deactivate')"
      :message="vm.t('products.deactivateConfirm', { name: vm.product?.name })"
      :confirm-text="vm.t('products.deactivate')"
      color="error"
      icon="mdi-package-variant-closed-remove"
      :loading="deactivating"
      @confirm="handleDeactivate"
    />

    <!-- Reactivate Confirmation -->
    <ConfirmDialog
      v-model="showReactivateDialog"
      :title="vm.t('products.reactivate')"
      :message="vm.t('products.reactivateConfirm', { name: vm.product?.name })"
      :confirm-text="vm.t('products.reactivate')"
      color="success"
      icon="mdi-package-variant-closed-check"
      :loading="reactivating"
      @confirm="handleReactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { useProductDetailView } from '@features/inventory/composables/useProductDetailView';
import { searchProducts } from '@features/inventory/api/products';
import { useNotificationStore } from '@shared/stores/notification';
import type { ProductDto } from '@features/inventory/types/inventory';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const notification = useNotificationStore();
const vm = reactive(useProductDetailView());

const showDeactivateDialog = ref(false);
const showReactivateDialog = ref(false);
const deactivating = ref(false);
const reactivating = ref(false);

const showBomCreateForm = ref(false);
const showBomLineForm = ref(false);
const showAccessoryForm = ref(false);
const showSubstituteForm = ref(false);

const bomCreateFormRef = ref();
const bomLineFormRef = ref();
const accessoryFormRef = ref();
const substituteFormRef = ref();

const availableProducts = ref<ProductDto[]>([]);

const bomCreateForm = reactive({
  name: '',
  childProductId: null as number | null,
  quantity: 1,
});

const bomLineForm = reactive({
  childProductId: null as number | null,
  quantity: 1,
});

const accessoryForm = reactive({
  accessoryProductId: null as number | null,
});

const substituteForm = reactive({
  substituteProductId: null as number | null,
});

const bomHeaders = computed(() => [
  { title: vm.t('products.form.name'), key: 'childProductName', sortable: false },
  { title: 'Quantity', key: 'quantity', sortable: false },
  { title: vm.t('common.actions'), key: 'actions', sortable: false, align: 'end' as const, width: '80px' },
]);

const requiredRule = (v: string | number) => (v !== '' && v !== null && v !== undefined) || vm.t('common.required');
const requiredSelectRule = (v: number | null) => v !== null || vm.t('common.required');
const positiveNumberRule = (v: number) => (typeof v === 'number' && v > 0) || 'Must be greater than 0';

onMounted(async () => {
  try {
    const response = await searchProducts({ includeDeleted: false, sortBy: 'name', sortDescending: false, page: 1, pageSize: 1000 });
    availableProducts.value = response.items;
  } catch {
    // silent — product selection will be empty
  }
});

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

async function handleReactivate(): Promise<void> {
  reactivating.value = true;
  try {
    await vm.handleReactivate();
    showReactivateDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    reactivating.value = false;
  }
}

async function submitCreateBom(): Promise<void> {
  const { valid } = await bomCreateFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleCreateBom({
      parentProductId: vm.productId,
      name: bomCreateForm.name || null,
      lines: [{ childProductId: bomCreateForm.childProductId!, quantity: bomCreateForm.quantity }],
    });
    showBomCreateForm.value = false;
    resetBomCreateForm();
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function submitAddBomLine(): Promise<void> {
  if (!vm.bom) return;
  const { valid } = await bomLineFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleAddBomLine(vm.bom.id, {
      childProductId: bomLineForm.childProductId!,
      quantity: bomLineForm.quantity,
    });
    showBomLineForm.value = false;
    resetBomLineForm();
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function handleRemoveBomLine(lineId: number): Promise<void> {
  if (!vm.bom) return;
  try {
    await vm.handleRemoveBomLine(vm.bom.id, lineId);
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function submitAddAccessory(): Promise<void> {
  const { valid } = await accessoryFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleAddAccessory({
      productId: vm.productId,
      accessoryProductId: accessoryForm.accessoryProductId!,
    });
    showAccessoryForm.value = false;
    resetAccessoryForm();
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function handleDeleteAccessory(accessoryId: number): Promise<void> {
  try {
    await vm.handleDeleteAccessory(accessoryId);
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function submitAddSubstitute(): Promise<void> {
  const { valid } = await substituteFormRef.value.validate();
  if (!valid) return;
  try {
    await vm.handleAddSubstitute({
      productId: vm.productId,
      substituteProductId: substituteForm.substituteProductId!,
    });
    showSubstituteForm.value = false;
    resetSubstituteForm();
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

async function handleDeleteSubstitute(substituteId: number): Promise<void> {
  try {
    await vm.handleDeleteSubstitute(substituteId);
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  }
}

function resetBomCreateForm(): void {
  bomCreateForm.name = '';
  bomCreateForm.childProductId = null;
  bomCreateForm.quantity = 1;
}

function resetBomLineForm(): void {
  bomLineForm.childProductId = null;
  bomLineForm.quantity = 1;
}

function resetAccessoryForm(): void {
  accessoryForm.accessoryProductId = null;
}

function resetSubstituteForm(): void {
  substituteForm.substituteProductId = null;
}
</script>
