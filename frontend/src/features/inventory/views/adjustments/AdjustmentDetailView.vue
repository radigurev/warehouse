<template>
  <div>
    <v-alert v-if="vm.notFound" type="warning" variant="tonal" class="ma-4">
      {{ vm.t('pageTitle.notFoundMessage') }}
      <template #append>
        <v-btn variant="text" @click="vm.goBack">{{ vm.t('pageTitle.back') }}</v-btn>
      </template>
    </v-alert>

    <v-progress-linear v-else-if="vm.loading" indeterminate color="primary" />

    <template v-else-if="vm.adjustment">
      <div class="d-flex align-center mb-4">
        <v-btn icon="mdi-arrow-left" variant="text" @click="vm.goBack" />
        <div class="ml-2">
          <div class="text-h6">{{ vm.t('adjustments.title') }} #{{ vm.adjustment.id }}</div>
        </div>
        <v-spacer />
        <v-chip :color="vm.statusColor(vm.adjustment.status)" size="small" variant="flat" class="mr-2">
          {{ vm.translateStatus(vm.adjustment.status) }}
        </v-chip>
        <v-btn v-if="vm.isPending" color="info" variant="tonal" prepend-icon="mdi-check" class="mr-2" @click="showApproveDialog = true">
          {{ vm.t('adjustments.approve') }}
        </v-btn>
        <v-btn v-if="vm.isPending" color="error" variant="tonal" prepend-icon="mdi-close" @click="showRejectDialog = true">
          {{ vm.t('adjustments.reject') }}
        </v-btn>
        <v-btn v-if="vm.isApproved" color="success" variant="tonal" prepend-icon="mdi-check-all" @click="showApplyDialog = true">
          {{ vm.t('adjustments.apply') }}
        </v-btn>
      </div>

      <!-- Info Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-information" class="mr-2" />
          {{ vm.t('adjustments.detail.info') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.columns.reason') }}</div>
              <div>{{ vm.adjustment.reason }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.columns.status') }}</div>
              <div>
                <v-chip :color="vm.statusColor(vm.adjustment.status)" size="small" variant="flat">
                  {{ vm.translateStatus(vm.adjustment.status) }}
                </v-chip>
              </div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.columns.createdAt') }}</div>
              <div>{{ vm.formatDate(vm.adjustment.createdAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.adjustment.notes" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.form.notes') }}</div>
              <div class="text-body-2">{{ vm.adjustment.notes }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Approval Info Card -->
      <v-card v-if="vm.adjustment.approvedAtUtc || vm.adjustment.rejectedAtUtc" :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-shield-check" class="mr-2" />
          {{ vm.t('adjustments.detail.approval') }}
        </v-card-title>
        <v-card-text>
          <v-row dense>
            <v-col v-if="vm.adjustment.approvedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.approve') }}</div>
              <div>{{ vm.formatDate(vm.adjustment.approvedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.adjustment.appliedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.apply') }}</div>
              <div>{{ vm.formatDate(vm.adjustment.appliedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.adjustment.rejectedAtUtc" cols="12" md="6">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.reject') }}</div>
              <div>{{ vm.formatDate(vm.adjustment.rejectedAtUtc) }}</div>
            </v-col>
            <v-col v-if="vm.adjustment.rejectionReason" cols="12">
              <div class="text-caption text-medium-emphasis">{{ vm.t('adjustments.form.rejectionReason') }}</div>
              <div class="text-body-2">{{ vm.adjustment.rejectionReason }}</div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>

      <!-- Lines Card -->
      <v-card :class="vm.layout.isCompact ? 'mb-2' : 'mb-4'">
        <v-card-title class="text-subtitle-1 font-weight-medium">
          <v-icon icon="mdi-format-list-bulleted" class="mr-2" />
          {{ vm.t('adjustments.detail.lines') }}
        </v-card-title>
        <v-card-text v-if="vm.adjustment.lines.length === 0" class="text-medium-emphasis">
          {{ vm.t('adjustments.detail.noLines') }}
        </v-card-text>
        <v-table v-else :density="vm.layout.vuetifyDensity">
          <thead>
            <tr>
              <th>{{ vm.t('adjustments.lines.product') }}</th>
              <th>{{ vm.t('adjustments.lines.location') }}</th>
              <th class="text-end">{{ vm.t('adjustments.lines.expected') }}</th>
              <th class="text-end">{{ vm.t('adjustments.lines.actual') }}</th>
              <th class="text-end">{{ vm.t('adjustments.lines.variance') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in vm.adjustment.lines" :key="line.id">
              <td>{{ line.productName }}</td>
              <td>{{ line.locationName || '\u2014' }}</td>
              <td class="text-end">{{ line.expectedQuantity }}</td>
              <td class="text-end">{{ line.actualQuantity }}</td>
              <td class="text-end">
                <span :class="vm.varianceColor(line.variance) ? `text-${vm.varianceColor(line.variance)}` : ''">
                  {{ line.variance > 0 ? '+' : '' }}{{ line.variance }}
                </span>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </template>

    <!-- Approve Confirmation -->
    <ConfirmDialog
      v-model="showApproveDialog"
      :title="vm.t('adjustments.approve')"
      :message="vm.t('adjustments.approveConfirm')"
      :confirm-text="vm.t('adjustments.approve')"
      color="info"
      icon="mdi-check"
      :loading="approving"
      @confirm="handleApprove"
    />

    <!-- Reject Dialog -->
    <FormWrapper v-model="showRejectDialog" max-width="450" :title="vm.t('adjustments.reject')" icon="mdi-close">
      <v-card-text>
        <v-form ref="rejectFormRef" @submit.prevent="handleReject">
          <v-textarea
            v-model="rejectionReason"
            :label="vm.t('adjustments.form.rejectionReason')"
            :density="vm.layout.vuetifyDensity"
            :rules="[requiredRule]"
            rows="3"
            auto-grow
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showRejectDialog = false">{{ vm.t('common.cancel') }}</v-btn>
        <v-btn color="error" variant="flat" :loading="rejecting" @click="handleReject">{{ vm.t('adjustments.reject') }}</v-btn>
      </v-card-actions>
    </FormWrapper>

    <!-- Apply Confirmation -->
    <ConfirmDialog
      v-model="showApplyDialog"
      :title="vm.t('adjustments.apply')"
      :message="vm.t('adjustments.applyConfirm')"
      :confirm-text="vm.t('adjustments.apply')"
      color="success"
      icon="mdi-check-all"
      :loading="applying"
      @confirm="handleApply"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useAdjustmentDetailView } from '@features/inventory/composables/useAdjustmentDetailView';
import { useNotificationStore } from '@shared/stores/notification';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';

const notification = useNotificationStore();
const vm = reactive(useAdjustmentDetailView());

const showApproveDialog = ref(false);
const showRejectDialog = ref(false);
const showApplyDialog = ref(false);
const approving = ref(false);
const rejecting = ref(false);
const applying = ref(false);
const rejectionReason = ref('');
const rejectFormRef = ref();

const requiredRule = (v: string) => !!v || vm.t('common.required');

async function handleApprove(): Promise<void> {
  approving.value = true;
  try {
    await vm.handleApprove();
    showApproveDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    approving.value = false;
  }
}

async function handleReject(): Promise<void> {
  const { valid } = await rejectFormRef.value.validate();
  if (!valid) return;
  rejecting.value = true;
  try {
    await vm.handleReject(rejectionReason.value);
    showRejectDialog.value = false;
    rejectionReason.value = '';
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    rejecting.value = false;
  }
}

async function handleApply(): Promise<void> {
  applying.value = true;
  try {
    await vm.handleApply();
    showApplyDialog.value = false;
  } catch {
    notification.error(vm.t('errors.UNEXPECTED_ERROR'));
  } finally {
    applying.value = false;
  }
}
</script>
