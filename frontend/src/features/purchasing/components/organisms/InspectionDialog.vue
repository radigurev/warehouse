<template>
  <v-dialog v-model="visible" max-width="500" persistent>
    <v-card>
      <v-card-title class="text-h6">
        <v-icon class="mr-2">mdi-clipboard-check</v-icon>
        {{ t('goodsReceipts.inspection.title') }}
      </v-card-title>

      <v-card-text>
        <v-row dense class="mb-4">
          <v-col cols="12">
            <div class="text-body-2 text-medium-emphasis mb-1">{{ t('goodsReceipts.lines.product') }}</div>
            <div class="text-body-1">Product #{{ props.line.purchaseOrderLineId }}</div>
          </v-col>
          <v-col cols="6">
            <div class="text-body-2 text-medium-emphasis mb-1">{{ t('goodsReceipts.lines.receivedQuantity') }}</div>
            <div class="text-body-1">{{ props.line.receivedQuantity }}</div>
          </v-col>
          <v-col v-if="props.line.batchNumber" cols="6">
            <div class="text-body-2 text-medium-emphasis mb-1">{{ t('goodsReceipts.lines.batchNumber') }}</div>
            <div class="text-body-1">{{ props.line.batchNumber }}</div>
          </v-col>
        </v-row>

        <v-divider class="mb-4" />

        <v-form ref="formRef" @submit.prevent="handleSubmit">
          <v-radio-group
            v-model="form.inspectionStatus"
            :label="t('goodsReceipts.inspection.status')"
            :rules="[rules.required]"
          >
            <v-radio label="Accepted" value="Accepted" color="success" />
            <v-radio label="Rejected" value="Rejected" color="error" />
            <v-radio label="Quarantined" value="Quarantined" color="warning" />
          </v-radio-group>

          <v-textarea
            v-model="form.inspectionNote"
            :label="t('goodsReceipts.inspection.note')"
            prepend-inner-icon="mdi-note-text"
            density="compact"
            :rules="[rules.notesLength]"
            rows="3"
            auto-grow
          />
        </v-form>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="cancel">{{ t('common.cancel') }}</v-btn>
        <v-btn color="primary" variant="flat" :loading="loading" @click="handleSubmit">
          {{ t('common.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@shared/stores/notification';
import { getApiErrorMessage } from '@shared/utils/getApiErrorMessage';
import { inspectReceiptLine } from '@features/purchasing/api/goods-receipts';
import type { GoodsReceiptLineDto } from '@features/purchasing/types/purchasing';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  receiptId: number;
  lineId: number;
  line: GoodsReceiptLineDto;
}>();

const emit = defineEmits<{
  saved: [];
}>();

const formRef = ref();
const loading = ref(false);

const form = reactive({
  inspectionStatus: '' as string,
  inspectionNote: '',
});

watch(visible, (val) => {
  if (val) {
    form.inspectionStatus = '';
    form.inspectionNote = '';
  }
});

const rules = {
  required: (v: string) => !!v || t('common.required'),
  notesLength: (v: string) => !v || v.length <= 2000 || t('validation.notesLength'),
};

async function handleSubmit(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  try {
    await inspectReceiptLine(props.receiptId, props.lineId, {
      inspectionStatus: form.inspectionStatus,
      inspectionNote: form.inspectionNote || null,
    });
    notification.success(t('goodsReceipts.inspection.title') + ' \u2713');
    visible.value = false;
    emit('saved');
  } catch (err) {
    notification.error(getApiErrorMessage(err, t));
  } finally {
    loading.value = false;
  }
}

function cancel(): void {
  visible.value = false;
}
</script>
