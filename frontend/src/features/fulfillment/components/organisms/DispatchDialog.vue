<template>
  <FormWrapper v-model="visible" max-width="500" :title="t('salesOrders.dispatch')" icon="mdi-truck-fast">
    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleDispatch">
        <v-select
          v-model="form.carrierId"
          :label="t('salesOrders.form.carrier')"
          :density="layout.vuetifyDensity"
          :items="carriers"
          item-title="name"
          item-value="id"
          :loading="carriersLoading"
          prepend-inner-icon="mdi-truck-delivery"
          clearable
          @update:model-value="onCarrierChanged"
        />
        <v-select
          v-model="form.carrierServiceLevelId"
          :label="t('salesOrders.form.serviceLevel')"
          :density="layout.vuetifyDensity"
          :items="serviceLevels"
          item-title="name"
          item-value="id"
          prepend-inner-icon="mdi-speedometer"
          clearable
          :disabled="!form.carrierId"
        />
        <v-textarea
          v-model="form.notes"
          :label="t('salesOrders.form.notes')"
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
      <v-btn color="teal" variant="flat" :loading="loading" @click="handleDispatch">
        {{ t('salesOrders.dispatch') }}
      </v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { createShipment } from '@features/fulfillment/api/shipments';
import { searchCarriers, getCarrierById } from '@features/fulfillment/api/carriers';
import type { SalesOrderDetailDto, CarrierDto, CarrierServiceLevelDto } from '@features/fulfillment/types/fulfillment';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  salesOrder?: SalesOrderDetailDto | null;
}>();

const emit = defineEmits<{
  dispatched: [];
}>();

const formRef = ref();
const loading = ref(false);
const carriers = ref<CarrierDto[]>([]);
const carriersLoading = ref(false);
const serviceLevels = ref<CarrierServiceLevelDto[]>([]);

const form = reactive({
  carrierId: null as number | null,
  carrierServiceLevelId: null as number | null,
  notes: '',
});

async function loadCarriers(): Promise<void> {
  carriersLoading.value = true;
  try {
    const result = await searchCarriers({ isActive: true, sortDescending: false, page: 1, pageSize: 1000 });
    carriers.value = result.items;
  } catch {
    // silent
  } finally {
    carriersLoading.value = false;
  }
}

onMounted(() => loadCarriers());

watch(visible, async (val) => {
  if (val && props.salesOrder) {
    form.carrierId = props.salesOrder.carrierId;
    form.carrierServiceLevelId = props.salesOrder.carrierServiceLevelId;
    form.notes = '';
    if (props.salesOrder.carrierId) {
      try {
        const detail = await getCarrierById(props.salesOrder.carrierId);
        serviceLevels.value = detail.serviceLevels;
      } catch {
        serviceLevels.value = [];
      }
    }
  } else if (val) {
    form.carrierId = null;
    form.carrierServiceLevelId = null;
    form.notes = '';
    serviceLevels.value = [];
  }
});

async function onCarrierChanged(): Promise<void> {
  form.carrierServiceLevelId = null;
  serviceLevels.value = [];
  if (!form.carrierId) return;
  try {
    const detail = await getCarrierById(form.carrierId);
    serviceLevels.value = detail.serviceLevels;
  } catch {
    serviceLevels.value = [];
  }
}

async function handleDispatch(): Promise<void> {
  if (!props.salesOrder) return;

  loading.value = true;
  try {
    await createShipment({
      salesOrderId: props.salesOrder.id,
      carrierId: form.carrierId,
      carrierServiceLevelId: form.carrierServiceLevelId,
      notes: form.notes || null,
    });
    notification.success(t('salesOrders.dispatch') + ' \u2713');
    visible.value = false;
    emit('dispatched');
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}
</script>
