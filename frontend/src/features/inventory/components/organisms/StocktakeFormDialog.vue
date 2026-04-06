<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="500" :title="t('stocktake.create')" icon="mdi-clipboard-check" @back="cancel">
    <v-card-title class="text-h6">
      {{ t('stocktake.create') }}
    </v-card-title>

    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handleSubmit">
        <v-row dense>
          <v-col v-bind="grid.fieldCols">
            <v-text-field
              v-model="form.name"
              :label="t('stocktake.form.name')"
              prepend-inner-icon="mdi-clipboard-text"
              :density="layout.vuetifyDensity"
              :rules="[rules.required, rules.nameLength]"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.warehouseId"
              :label="t('stocktake.form.warehouse')"
              prepend-inner-icon="mdi-warehouse"
              :density="layout.vuetifyDensity"
              :items="warehouses"
              item-title="name"
              item-value="id"
              :loading="warehousesLoading"
              :rules="[rules.required]"
              @update:model-value="onWarehouseChanged"
            />
          </v-col>

          <v-col v-bind="grid.fieldCols">
            <v-autocomplete
              v-model="form.zoneId"
              :label="t('stocktake.form.zone')"
              prepend-inner-icon="mdi-select-group"
              :density="layout.vuetifyDensity"
              :items="zones"
              item-title="name"
              item-value="id"
              :loading="zonesLoading"
              clearable
              :disabled="!form.warehouseId"
            />
          </v-col>

          <v-col v-bind="grid.fullCols">
            <v-textarea
              v-model="form.notes"
              :label="t('stocktake.form.notes')"
              prepend-inner-icon="mdi-note-text"
              :density="layout.vuetifyDensity"
              rows="2"
              auto-grow
            />
          </v-col>
        </v-row>
      </v-form>
    </v-card-text>

    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="cancel">{{ t('common.cancel') }}</v-btn>
      <v-btn color="primary" variant="flat" :loading="submitting" @click="handleSubmit">
        {{ t('common.save') }}
      </v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { useFormGrid } from '@shared/composables/useFormGrid';
import { searchWarehouses } from '@features/inventory/api/warehouses';
import { searchZones } from '@features/inventory/api/zones';
import { createSession } from '@features/inventory/api/stocktake';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import type { WarehouseDto, ZoneDto } from '@features/inventory/types/inventory';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();
const grid = useFormGrid();

const visible = defineModel<boolean>({ required: true });

defineProps<{
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  saved: [];
  cancelled: [];
}>();

const formRef = ref();
const submitting = ref(false);

const warehouses = ref<WarehouseDto[]>([]);
const warehousesLoading = ref(false);
const zones = ref<ZoneDto[]>([]);
const zonesLoading = ref(false);

const form = reactive({
  name: '',
  warehouseId: null as number | null,
  zoneId: null as number | null,
  notes: '',
});

const rules = {
  required: (v: unknown) => (v !== null && v !== undefined && v !== '') || t('common.required'),
  nameLength: (v: string) => !v || v.length <= 200 || t('validation.nameTooLong'),
};

async function loadWarehouses(): Promise<void> {
  warehousesLoading.value = true;
  try {
    const response = await searchWarehouses({ includeDeleted: false, sortDescending: false, page: 1, pageSize: 500 });
    warehouses.value = response.items;
  } catch {
    // dropdown empty
  } finally {
    warehousesLoading.value = false;
  }
}

async function onWarehouseChanged(): Promise<void> {
  form.zoneId = null;
  zones.value = [];
  if (form.warehouseId) {
    zonesLoading.value = true;
    try {
      const response = await searchZones({ warehouseId: form.warehouseId, page: 1, pageSize: 500 });
      zones.value = response.items;
    } catch {
      zones.value = [];
    } finally {
      zonesLoading.value = false;
    }
  }
}

onMounted(() => loadWarehouses());

watch(visible, (val) => {
  if (val) resetForm();
});

function resetForm(): void {
  form.name = '';
  form.warehouseId = null;
  form.zoneId = null;
  form.notes = '';
  zones.value = [];
}

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  submitting.value = true;
  try {
    await createSession({
      warehouseId: form.warehouseId!,
      zoneId: form.zoneId ?? undefined,
      name: form.name,
      notes: form.notes || undefined,
    });
    notification.success(t('stocktake.create') + ' \u2713');
    visible.value = false;
    emit('saved');
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    submitting.value = false;
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
