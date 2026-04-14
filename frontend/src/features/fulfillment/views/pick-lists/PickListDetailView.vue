<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.pickList">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.pickList.pickListNumber }}</div>
          <div class="text-caption text-medium-emphasis">{{ vm.t('pickLists.detail.forSo') }} {{ vm.pickList.salesOrderNumber }}</div>
        </div>
        <v-spacer />
        <v-chip :color="pickListStatusColor(vm.pickList.status)" size="small" label class="mr-2">
          {{ vm.t(`pickLists.status.${vm.pickList.status}`) }}
        </v-chip>
        <v-btn v-if="vm.pickList.status === 'Pending'" color="error" variant="tonal" prepend-icon="mdi-close" @click="showCancelDialog = true">
          {{ vm.t('pickLists.cancel') }}
        </v-btn>
      </div>

      <!-- Pick List Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('pickLists.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('pickLists.columns.salesOrderNumber') }}</div>
              <div>
                <a class="text-primary cursor-pointer" @click="vm.navigateToSalesOrder()">{{ vm.pickList.salesOrderNumber }}</a>
              </div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('pickLists.columns.warehouseName') }}</div>
              <div>{{ vm.pickList.warehouseName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('pickLists.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.pickList.createdAtUtc) }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('pickLists.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.pickList.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('pickLists.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('pickLists.lines.product') }}</th>
              <th>{{ vm.t('pickLists.lines.location') }}</th>
              <th class="text-end">{{ vm.t('pickLists.lines.requestedQty') }}</th>
              <th class="text-end">{{ vm.t('pickLists.lines.actualPickedQty') }}</th>
              <th>{{ vm.t('pickLists.lines.pickedBy') }}</th>
              <th>{{ vm.t('pickLists.lines.pickedAt') }}</th>
              <th>{{ vm.t('pickLists.lines.status') }}</th>
              <th style="width: 120px">{{ vm.t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.pickList.lines" :key="line.id">
              <td>{{ line.productCode }} - {{ line.productName }}</td>
              <td>{{ line.sourceLocationCode || '\u2014' }}</td>
              <td class="text-end">{{ line.requestedQuantity }}</td>
              <td class="text-end">{{ line.actualPickedQuantity ?? '\u2014' }}</td>
              <td>{{ line.pickedByUserId ?? '\u2014' }}</td>
              <td>{{ line.pickedAtUtc ? vm.formatDate(line.pickedAtUtc) : '\u2014' }}</td>
              <td>
                <v-chip :color="line.status === 'Picked' ? 'green' : 'grey'" size="small" label>
                  {{ vm.t(`pickLists.lineStatus.${line.status}`) }}
                </v-chip>
              </td>
              <td>
                <ActionChip
                  v-if="line.status === 'Pending' && vm.pickList!.status === 'Pending'"
                  :label="vm.t('pickLists.pick')"
                  icon="mdi-hand-pointing-right"
                  color="primary"
                  :compact="vm.layout.isCompact"
                  @click="openPickDialog(line)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Cancel Pick List Dialog -->
    <ConfirmDialog
      v-model="showCancelDialog"
      :title="vm.t('pickLists.cancel')"
      :message="vm.t('pickLists.cancelMessage', { pickListNumber: vm.pickList?.pickListNumber })"
      :confirm-text="vm.t('pickLists.cancel')"
      color="error"
      icon="mdi-close"
      :loading="cancelling"
      @confirm="handleCancel"
    />

    <!-- Pick Dialog -->
    <PickDialog
      v-model="showPickDialog"
      :line="pickingLine"
      @picked="onPicked"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { usePickListDetailView } from '@features/fulfillment/composables/usePickListDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import PickDialog from '@features/fulfillment/components/organisms/PickDialog.vue';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import type { PickListLineDto } from '@features/fulfillment/types/fulfillment';

const notification = useNotificationStore();
const vm = reactive(usePickListDetailView());

const showCancelDialog = ref(false);
const showPickDialog = ref(false);
const cancelling = ref(false);
const pickingLine = ref<PickListLineDto | null>(null);

function pickListStatusColor(status: string): string {
  const map: Record<string, string> = {
    Pending: 'grey',
    Completed: 'green',
    Cancelled: 'red',
  };
  return map[status] || 'grey';
}

function openPickDialog(line: PickListLineDto): void {
  pickingLine.value = line;
  showPickDialog.value = true;
}

async function handleCancel(): Promise<void> {
  cancelling.value = true;
  try {
    await vm.handleCancelPickList();
    showCancelDialog.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, vm.t));
  } finally {
    cancelling.value = false;
  }
}

async function onPicked(): Promise<void> {
  showPickDialog.value = false;
  pickingLine.value = null;
  await vm.loadPickList();
}
</script>
