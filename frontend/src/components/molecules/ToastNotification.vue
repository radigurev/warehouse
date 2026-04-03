<template>
  <v-snackbar
    v-model="notification.visible"
    :timeout="notification.timeout"
    :color="toastColor"
    :location="isCritical ? 'bottom' : 'top right'"
    :variant="isCritical ? 'flat' : 'tonal'"
    :rounded="isCritical ? undefined : 'pill'"
    :min-width="isCritical ? undefined : '300'"
    :max-width="isCritical ? undefined : '450'"
  >
    <div class="d-flex align-center">
      <v-icon :icon="iconForType" class="mr-2" size="small" />
      {{ notification.message }}
    </div>
    <template #actions>
      <v-btn variant="text" size="small" @click="notification.hide()">
        {{ t('common.close') }}
      </v-btn>
    </template>
  </v-snackbar>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@/stores/notification';

const { t } = useI18n();
const notification = useNotificationStore();

const isCritical = computed(() => notification.type === 'error');

const toastColor = computed(() => {
  if (isCritical.value) return 'error';
  if (notification.type === 'warning') return 'warning';
  return undefined;
});

const iconForType = computed(() => {
  switch (notification.type) {
    case 'success': return 'mdi-check-circle';
    case 'error': return 'mdi-alert-circle';
    case 'warning': return 'mdi-alert';
    case 'info': return 'mdi-information';
    default: return 'mdi-information';
  }
});
</script>
