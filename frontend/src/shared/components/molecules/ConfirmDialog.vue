<template>
  <v-dialog v-model="visible" max-width="450" persistent>
    <v-card class="d-flex flex-column">
      <div class="confirm-header">
        <v-icon v-if="icon" :icon="icon" class="mr-2" />
        <span class="text-h6">{{ title }}</span>
        <v-spacer />
        <v-btn icon="mdi-close" variant="text" size="small" @click="cancel" />
      </div>

      <v-card-text class="py-6 px-6">
        <div class="d-flex align-start">
          <v-icon v-if="icon" :icon="icon" :color="color" size="28" class="mr-4 mt-1" />
          <div class="text-body-1">{{ message }}</div>
        </div>
      </v-card-text>

      <v-card-actions class="confirm-actions">
        <v-spacer />
        <v-btn variant="text" @click="cancel">{{ t('common.cancel') }}</v-btn>
        <v-btn :color="color" variant="flat" @click="confirm" :loading="loading">
          {{ confirmText || t('common.confirm') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const visible = defineModel<boolean>({ required: true });

defineProps<{
  title: string;
  message: string;
  confirmText?: string;
  color?: string;
  icon?: string;
  loading?: boolean;
}>();

const emit = defineEmits<{
  confirm: [];
  cancel: [];
}>();

function confirm(): void {
  emit('confirm');
}

function cancel(): void {
  visible.value = false;
  emit('cancel');
}
</script>

<style scoped>
.confirm-header {
  display: flex;
  align-items: center;
  padding: 10px 16px;
  background: #334155;
  color: white;
  font-size: 0.95rem;
}

.confirm-header :deep(.v-btn) {
  color: white;
}

.confirm-actions {
  background: #334155;
  color: white;
  padding: 12px 24px;
}

.confirm-actions :deep(.v-btn) {
  color: white;
}
</style>
