<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.po">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.po.orderNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.po.supplierName }}</div>
        </div>
        <v-spacer />
        <v-chip :color="poStatusColor(vm.po.status)" size="small" label class="mr-2">
          {{ vm.t(`purchaseOrders.status.${vm.po.status}`) }}
        </v-chip>
        <v-btn v-if="vm.po.status === 'Draft'" color="primary" variant="tonal" prepend-icon="mdi-pencil" class="mr-2" @click="handleEdit">
          {{ vm.t('common.edit') }}
        </v-btn>
        <v-btn v-if="vm.po.status === 'Draft'" color="success" variant="tonal" prepend-icon="mdi-check" class="mr-2" @click="showConfirmDialog = true">
          {{ vm.t('purchaseOrders.confirm') }}
        </v-btn>
        <v-btn v-if="vm.po.status === 'Draft' || vm.po.status === 'Confirmed'" color="error" variant="tonal" prepend-icon="mdi-close" class="mr-2" @click="showCancelDialog = true">
          {{ vm.t('purchaseOrders.cancel') }}
        </v-btn>
        <v-btn v-if="vm.po.status === 'Confirmed' || vm.po.status === 'PartiallyReceived'" color="primary" variant="tonal" prepend-icon="mdi-package-down" class="mr-2" @click="handleCreateGoodsReceipt">
          {{ vm.t('purchaseOrders.createGoodsReceipt') }}
        </v-btn>
        <v-btn v-if="vm.po.status === 'PartiallyReceived' || vm.po.status === 'Received'" color="blue-grey" variant="tonal" prepend-icon="mdi-lock" @click="showCloseDialog = true">
          {{ vm.t('purchaseOrders.close') }}
        </v-btn>
      </div>

      <!-- PO Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('purchaseOrders.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.columns.supplierName') }}</div>
              <div>{{ vm.po.supplierName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.columns.destinationWarehouse') }}</div>
              <div>{{ vm.po.destinationWarehouseId }}</div>
            </v-col>
            <v-col v-if="vm.po.expectedDeliveryDate" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.columns.expectedDeliveryDate') }}</div>
              <div>{{ vm.formatDate(vm.po.expectedDeliveryDate) }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.columns.totalAmount') }}</div>
              <div>{{ vm.po.totalAmount.toFixed(2) }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.po.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.po.confirmedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.detail.confirmedAt') }}</div>
              <div>{{ vm.formatDate(vm.po.confirmedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.po.closedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.detail.closedAt') }}</div>
              <div>{{ vm.formatDate(vm.po.closedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.po.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('purchaseOrders.form.notes') }}</div>
              <div class="text-body-2">{{ vm.po.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('purchaseOrders.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.po.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('purchaseOrders.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('purchaseOrders.lines.product') }}</th>
              <th class="text-end">{{ vm.t('purchaseOrders.lines.orderedQty') }}</th>
              <th class="text-end">{{ vm.t('purchaseOrders.lines.unitPrice') }}</th>
              <th class="text-end">{{ vm.t('purchaseOrders.lines.lineTotal') }}</th>
              <th class="text-end">{{ vm.t('purchaseOrders.lines.receivedQty') }}</th>
              <th class="text-end">{{ vm.t('purchaseOrders.lines.remaining') }}</th>
              <th style="width: 120px">{{ vm.t('purchaseOrders.lines.progress') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.po.lines" :key="line.id">
              <td>{{ line.productName }}</td>
              <td class="text-end">{{ line.orderedQuantity }}</td>
              <td class="text-end">{{ line.unitPrice.toFixed(2) }}</td>
              <td class="text-end">{{ line.lineTotal.toFixed(2) }}</td>
              <td class="text-end">{{ line.receivedQuantity }}</td>
              <td class="text-end">{{ line.remainingQuantity }}</td>
              <td>
                <v-progress-linear
                  :model-value="line.orderedQuantity > 0 ? (line.receivedQuantity / line.orderedQuantity) * 100 : 0"
                  :color="line.receivedQuantity >= line.orderedQuantity ? 'success' : 'primary'"
                  height="8"
                  rounded
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Confirm Dialog -->
    <ConfirmDialog
      v-model="showConfirmDialog"
      :title="vm.t('purchaseOrders.confirm')"
      :message="vm.t('purchaseOrders.confirmMessage', { orderNumber: vm.po?.orderNumber })"
      :confirm-text="vm.t('purchaseOrders.confirm')"
      color="success"
      icon="mdi-check"
      :loading="confirming"
      @confirm="handleConfirm"
    />

    <!-- Cancel Dialog -->
    <ConfirmDialog
      v-model="showCancelDialog"
      :title="vm.t('purchaseOrders.cancel')"
      :message="vm.t('purchaseOrders.cancelMessage', { orderNumber: vm.po?.orderNumber })"
      :confirm-text="vm.t('purchaseOrders.cancel')"
      color="error"
      icon="mdi-close"
      :loading="cancelling"
      @confirm="handleCancel"
    />

    <!-- Close Dialog -->
    <ConfirmDialog
      v-model="showCloseDialog"
      :title="vm.t('purchaseOrders.close')"
      :message="vm.t('purchaseOrders.closeMessage', { orderNumber: vm.po?.orderNumber })"
      :confirm-text="vm.t('purchaseOrders.close')"
      color="blue-grey"
      icon="mdi-lock"
      :loading="closing"
      @confirm="handleClose"
    />

    <!-- Goods Receipt Dialog (modal mode) -->
    <GoodsReceiptFormDialog v-model="showGoodsReceiptDialog" :pre-selected-po-id="vm.poId" @saved="onGoodsReceiptSaved" />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { usePurchaseOrderDetailView } from '@features/purchasing/composables/usePurchaseOrderDetailView';
import { useLayoutStore } from '@shared/stores/layout';
import { useNotificationStore } from '@shared/stores/notification';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import GoodsReceiptFormDialog from '@features/purchasing/components/organisms/GoodsReceiptFormDialog.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const router = useRouter();
const layout = useLayoutStore();
const notification = useNotificationStore();
const vm = reactive(usePurchaseOrderDetailView());

const showConfirmDialog = ref(false);
const showCancelDialog = ref(false);
const showCloseDialog = ref(false);
const showGoodsReceiptDialog = ref(false);
const confirming = ref(false);
const cancelling = ref(false);
const closing = ref(false);

function poStatusColor(status: string): string {
  const map: Record<string, string> = {
    Draft: 'grey',
    Confirmed: 'blue',
    PartiallyReceived: 'orange',
    Received: 'green',
    Closed: 'blue-grey',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}

function handleEdit(): void {
  router.push({ name: 'purchase-order-edit', params: { id: vm.poId } });
}

function handleCreateGoodsReceipt(): void {
  if (layout.isPageMode) {
    router.push({ name: 'goods-receipt-create', query: { poId: String(vm.poId), returnTo: String(vm.poId) } });
  } else {
    showGoodsReceiptDialog.value = true;
  }
}

async function onGoodsReceiptSaved(): Promise<void> {
  showGoodsReceiptDialog.value = false;
  await vm.loadPurchaseOrder();
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
</script>
