<template>
  <v-dialog v-model="visible" max-width="450" persistent>
    <v-card>
      <v-card-title class="text-h6">
        <v-icon :icon="icon" :color="color" class="mr-2" />
        {{ title }}
      </v-card-title>

      <v-card-text>{{ message }}</v-card-text>

      <v-card-actions>
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
