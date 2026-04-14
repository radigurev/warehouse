<template>
  <FormWrapper v-model="visible" max-width="450" :title="t('pickLists.pick')" icon="mdi-hand-pointing-right">
    <v-card-text>
      <v-form ref="formRef" @submit.prevent="handlePick">
        <v-text-field
          :model-value="line?.productCode + ' - ' + line?.productName"
          :label="t('pickLists.lines.product')"
          :density="layout.vuetifyDensity"
          readonly
          prepend-inner-icon="mdi-package-variant"
        />
        <v-text-field
          :model-value="line?.sourceLocationCode || '\u2014'"
          :label="t('pickLists.lines.location')"
          :density="layout.vuetifyDensity"
          readonly
          prepend-inner-icon="mdi-map-marker"
        />
        <v-text-field
          :model-value="String(line?.requestedQuantity ?? 0)"
          :label="t('pickLists.lines.requestedQty')"
          :density="layout.vuetifyDensity"
          readonly
          prepend-inner-icon="mdi-counter"
        />
        <v-text-field
          v-model.number="form.actualQuantity"
          :label="t('pickLists.lines.actualPickedQty')"
          :density="layout.vuetifyDensity"
          type="number"
          :min="0.01"
          step="any"
          prepend-inner-icon="mdi-hand-pointing-right"
          :rules="[rules.required, rules.positiveNumber]"
          autofocus
        />
      </v-form>
    </v-card-text>
    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="visible = false">{{ t('common.cancel') }}</v-btn>
      <v-btn color="primary" variant="flat" :loading="loading" @click="handlePick">
        {{ t('pickLists.pick') }}
      </v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { useLayoutStore } from '@shared/stores/layout';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import FormWrapper from '@shared/components/molecules/FormWrapper.vue';
import { pickLine } from '@features/fulfillment/api/pick-lists';
import type { PickListLineDto } from '@features/fulfillment/types/fulfillment';

const { t } = useI18n();
const notification = useNotificationStore();
const layout = useLayoutStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  line: PickListLineDto | null;
  pickListId: number | null;
}>();

const emit = defineEmits<{
  picked: [];
}>();

const formRef = ref();
const loading = ref(false);

const form = reactive({
  actualQuantity: 0,
});

watch(visible, (val) => {
  if (val && props.line) {
    form.actualQuantity = props.line.requestedQuantity;
  }
});

const rules = {
  required: (v: number) => (v !== null && v !== undefined && v > 0) || t('common.required'),
  positiveNumber: (v: number) => v > 0 || t('validation.mustBePositive'),
};

async function handlePick(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid || !props.line) return;

  loading.value = true;
  try {
    if (!props.pickListId) return;
    await pickLine(props.pickListId, props.line.id, { actualQuantity: form.actualQuantity });
    notification.success(t('pickLists.pick') + ' \u2713');
    visible.value = false;
    emit('picked');
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}
</script>
