<template>
  <FormWrapper v-model="visible" max-width="500" :title="t('shipments.updateStatus')" icon="mdi-update">
    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-select
          v-model="form.status"
          :label="t('shipments.statusDialog.newStatus')"
          :density="layout.vuetifyDensity"
          :items="validTransitions"
          item-title="label"
          item-value="value"
          prepend-inner-icon="mdi-truck-delivery"
          :rules="[rules.required]"
        />
        <v-text-field
          v-model="form.trackingNumber"
          :label="t('shipments.statusDialog.trackingNumber')"
          :density="layout.vuetifyDensity"
          prepend-inner-icon="mdi-barcode"
          clearable
        />
        <v-text-field
          v-model="form.trackingUrl"
          :label="t('shipments.statusDialog.trackingUrl')"
          :density="layout.vuetifyDensity"
          prepend-inner-icon="mdi-link"
          clearable
        />
        <v-textarea
          v-model="form.notes"
          :label="t('shipments.statusDialog.notes')"
          :density="layout.vuetifyDensity"
          rows="3"
          auto-grow
          prepend-inner-icon="mdi-note-text"
        />
      </v-form>
    </v-card-text>
    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="visible = false">{{ t('common.cancel') }}</v-btn>
      <v-btn color="primary" variant="flat" :loading="loading" @click="handleSubmit">
        {{ t('common.save') }}
      </v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import type { ShipmentStatus } from '@features/fulfillment/types/fulfillment';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  currentStatus: ShipmentStatus;
  shipmentId?: number;
}>();

const emit = defineEmits<{
  updated: [request: { status: string; trackingNumber?: string | null; trackingUrl?: string | null; notes?: string | null }];
}>();

const formRef = ref();
const loading = ref(false);

const form = reactive({
  status: '' as string,
  trackingNumber: '',
  trackingUrl: '',
  notes: '',
});

const statusTransitions: Record<string, string[]> = {
  Dispatched: ['InTransit', 'Failed', 'Returned'],
  InTransit: ['OutForDelivery', 'Delivered', 'Failed', 'Returned'],
  OutForDelivery: ['Delivered', 'Failed', 'Returned'],
  Failed: ['InTransit', 'Returned'],
};

const validTransitions = computed(() => {
  const transitions = statusTransitions[props.currentStatus] || [];
  return transitions.map((s) => ({
    value: s,
    label: t(`shipments.status.${s}`),
  }));
});

watch(visible, (val) => {
  if (val) {
    const transitions = statusTransitions[props.currentStatus] || [];
    form.status = transitions.length > 0 ? transitions[0] : '';
    form.trackingNumber = '';
    form.trackingUrl = '';
    form.notes = '';
  }
});

const rules = {
  required: (v: string) => !!v || t('common.required'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    emit('updated', {
      status: form.status,
      trackingNumber: form.trackingNumber || null,
      trackingUrl: form.trackingUrl || null,
      notes: form.notes || null,
    });
    visible.value = false;
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}
</script>
