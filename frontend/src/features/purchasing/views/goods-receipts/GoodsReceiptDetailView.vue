<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.receipt">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.receipt.receiptNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.t('goodsReceipts.detail.forPo') }} {{ vm.receipt.purchaseOrderNumber }}</div>
        </div>
        <v-spacer />
        <v-btn v-if="vm.receipt.status !== 'Completed'" color="success" variant="tonal" prepend-icon="mdi-check" @click="showCompleteDialog = true">
          {{ vm.t('goodsReceipts.complete') }}
        </v-btn>
      </div>

      <!-- Receipt Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('goodsReceipts.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('goodsReceipts.columns.receiptNumber') }}</div>
              <div>{{ vm.receipt.receiptNumber }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('goodsReceipts.columns.purchaseOrderNumber') }}</div>
              <div>
                <a class="text-primary cursor-pointer" @click="goToPurchaseOrder">{{ vm.receipt.purchaseOrderNumber }}</a>
              </div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('goodsReceipts.columns.warehouse') }}</div>
              <div>{{ vm.receipt.warehouseId }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('goodsReceipts.columns.receivedAt') }}</div>
              <div>{{ vm.formatDate(vm.receipt.receivedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.receipt.completedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('goodsReceipts.detail.completedAt') }}</div>
              <div>{{ vm.formatDate(vm.receipt.completedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.receipt.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('goodsReceipts.form.notes') }}</div>
              <div class="text-body-2">{{ vm.receipt.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('goodsReceipts.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.receipt.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('goodsReceipts.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('goodsReceipts.lines.poLine') }}</th>
              <th class="text-end">{{ vm.t('goodsReceipts.lines.receivedQty') }}</th>
              <th>{{ vm.t('goodsReceipts.lines.batchNumber') }}</th>
              <th>{{ vm.t('goodsReceipts.lines.inspectionStatus') }}</th>
              <th>{{ vm.t('goodsReceipts.lines.inspectionNote') }}</th>
              <th style="width: 200px">{{ vm.t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.receipt.lines" :key="line.id">
              <td>{{ line.purchaseOrderLineId }}</td>
              <td class="text-end">{{ line.receivedQuantity }}</td>
              <td>{{ line.batchNumber || '\u2014' }}</td>
              <td>
                <v-chip :color="inspectionStatusColor(line.inspectionStatus)" size="small" label>
                  {{ vm.t(`goodsReceipts.inspectionStatus.${line.inspectionStatus}`) }}
                </v-chip>
              </td>
              <td>{{ line.inspectionNote || '\u2014' }}</td>
              <td>
                <ActionChip
                  v-if="line.inspectionStatus === 'Pending'"
                  :label="vm.t('goodsReceipts.inspect')"
                  icon="mdi-clipboard-check-outline"
                  color="primary"
                  :compact="vm.layout.isCompact"
                  @click="openInspectDialog(line)"
                />
                <ActionChip
                  v-if="line.inspectionStatus === 'Quarantined'"
                  :label="vm.t('goodsReceipts.resolve')"
                  icon="mdi-clipboard-edit-outline"
                  color="orange"
                  :compact="vm.layout.isCompact"
                  @click="openResolveDialog(line)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Complete Dialog -->
    <ConfirmDialog
      v-model="showCompleteDialog"
      :title="vm.t('goodsReceipts.complete')"
      :message="vm.t('goodsReceipts.completeConfirm')"
      :confirm-text="vm.t('goodsReceipts.complete')"
      color="success"
      icon="mdi-check"
      :loading="completing"
      @confirm="handleComplete"
    />

    <!-- Inspect Dialog -->
    <FormWrapper v-model="showInspectDialog" max-width="450" :title="vm.t('goodsReceipts.inspect')" icon="mdi-clipboard-check-outline">
      <v-card-text>
        <v-form ref="inspectFormRef" @submit.prevent="submitInspect">
          <v-select
            v-model="inspectForm.inspectionStatus"
            :label="vm.t('goodsReceipts.lines.inspectionStatus')"
            :items="['Accepted', 'Rejected', 'Quarantined']"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule]"
          />
          <v-textarea
            v-model="inspectForm.inspectionNote"
            :label="vm.t('goodsReceipts.lines.inspectionNote')"
            :density="vm.layout.vuetifyDensity"
            rows="3"
            auto-grow
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showInspectDialog = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" :loading="inspecting" @click="submitInspect">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Resolve Quarantine Dialog -->
    <FormWrapper v-model="showResolveDialog" max-width="450" :title="vm.t('goodsReceipts.resolve')" icon="mdi-clipboard-edit-outline">
      <v-card-text>
        <v-form ref="resolveFormRef" @submit.prevent="submitResolve">
          <v-select
            v-model="resolveForm.resolution"
            :label="vm.t('goodsReceipts.resolveResolution')"
            :items="['Accepted', 'Rejected']"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule]"
          />
          <v-textarea
            v-model="resolveForm.note"
            :label="vm.t('goodsReceipts.resolveNote')"
            :density="vm.layout.vuetifyDensity"
            rows="3"
            auto-grow
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showResolveDialog = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="orange" variant="flat" :loading="resolving" @click="submitResolve">{{ vm.t('common.save') }}</v-btn>
      </v-card-actions>
    </FormWrapper>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { useGoodsReceiptDetailView } from '@features/purchasing/composables/useGoodsReceiptDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import type { GoodsReceiptLineDto } from '@features/purchasing/types/purchasing';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';

const router = useRouter();
const notification = useNotificationStore();
const vm = reactive(useGoodsReceiptDetailView());

const showCompleteDialog = ref(false);
const completing = ref(false);

const showInspectDialog = ref(false);
const inspecting = ref(false);
const inspectingLineId = ref<number | null>(null);
const inspectFormRef = ref();

const showResolveDialog = ref(false);
const resolving = ref(false);
const resolvingLineId = ref<number | null>(null);
const resolveFormRef = ref();

const inspectForm = reactive({
  inspectionStatus: 'Accepted',
  inspectionNote: '',
});

const resolveForm = reactive({
  resolution: 'Accepted',
  note: '',
});

const requiredRule = (v: string) => !!v || vm.t('common.required');

function inspectionStatusColor(status: string): string {
  const map: Record<string, string> = {
    Pending: 'grey',
    Accepted: 'green',
    Rejected: 'red',
    Quarantined: 'orange',
  };
  return map[status] || 'grey';
}

function goToPurchaseOrder(): void {
  if (vm.receipt) {
    router.push({ name: 'purchase-order-detail', params: { id: vm.receipt.purchaseOrderId } });
  }
}

function openInspectDialog(line: GoodsReceiptLineDto): void {
  inspectingLineId.value = line.id;
  inspectForm.inspectionStatus = 'Accepted';
  inspectForm.inspectionNote = '';
  showInspectDialog.value = true;
}

function openResolveDialog(line: GoodsReceiptLineDto): void {
  resolvingLineId.value = line.id;
  resolveForm.resolution = 'Accepted';
  resolveForm.note = '';
  showResolveDialog.value = true;
}

async function handleComplete(): Promise<void> {
  completing.value = true;
  try {
    await vm.handleComplete();
    showCompleteDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    completing.value = false;
  }
}

async function submitInspect(): Promise<void> {
  const { valid } = await inspectFormRef.value.validate();
  if (!valid || !inspectingLineId.value) return;
  inspecting.value = true;
  try {
    await vm.handleInspect(inspectingLineId.value, {
      inspectionStatus: inspectForm.inspectionStatus,
      inspectionNote: inspectForm.inspectionNote || null,
    });
    showInspectDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    inspecting.value = false;
  }
}

async function submitResolve(): Promise<void> {
  const { valid } = await resolveFormRef.value.validate();
  if (!valid || !resolvingLineId.value) return;
  resolving.value = true;
  try {
    await vm.handleResolve(resolvingLineId.value, {
      resolution: resolveForm.resolution,
      note: resolveForm.note || null,
    });
    showResolveDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    resolving.value = false;
  }
}
</script>
