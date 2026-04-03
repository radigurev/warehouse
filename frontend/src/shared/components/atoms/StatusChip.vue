<template>
  <v-chip :color="active ? 'success' : 'grey'" size="small" variant="flat">
    {{ active ? t('common.active') : t('common.inactive') }}
  </v-chip>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

defineProps<{
  active: boolean;
}>();
</script>
