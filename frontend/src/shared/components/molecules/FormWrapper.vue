<template>
  <v-dialog v-if="mode !== 'page'" v-model="model" :max-width="maxWidth" persistent scrollable>
    <v-card class="d-flex flex-column" style="max-height: 85vh">
      <div class="modal-header">
        <v-icon v-if="icon" :icon="icon" class="mr-2" />
        <span class="text-h6">{{ title }}</span>
        <v-spacer />
        <v-btn icon="mdi-close" variant="text" size="small" @click="model = false" />
      </div>
      <div class="flex-grow-1 overflow-y-auto hide-card-title">
        <slot />
      </div>
    </v-card>
  </v-dialog>

  <div v-else class="page-form">
    <div class="page-back-bar">
      <v-btn variant="text" prepend-icon="mdi-arrow-left" size="small" @click="$emit('back')">
        {{ backLabel }}
      </v-btn>
    </div>
    <v-card flat class="page-form-content hide-card-title">
      <slot />
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
const model = defineModel<boolean>({ required: true });

defineProps<{
  mode?: 'dialog' | 'page';
  maxWidth?: string | number;
  title?: string;
  icon?: string;
}>();

defineEmits<{
  back: [];
}>();

const backLabel = t('pageTitle.back');
</script>

<style scoped>
.modal-header {
  display: flex;
  align-items: center;
  padding: 10px 16px;
  background: #334155;
  color: white;
  font-size: 0.95rem;
}

.modal-header :deep(.v-btn) {
  color: white;
}

.page-form {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow: hidden;
}

.page-form-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  border-radius: 0 !important;
}

.page-back-bar {
  padding: 8px 16px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}

.page-form-content :deep(.v-card-text) {
  flex: 1;
  padding-top: 24px;
}

.page-form-content :deep(.v-card-actions) {
  margin-top: auto;
  border-radius: 0;
}

.hide-card-title :deep(.v-card-title) {
  display: none;
}

.hide-card-title :deep(.v-card-text) {
  padding-top: 20px;
  padding-bottom: 20px;
}

.hide-card-title :deep(.v-card-text .v-input) {
  margin-bottom: 6px;
}

:deep(.v-card-actions) {
  background: #334155;
  color: white;
  padding: 12px 24px;
  z-index: 1;
}

:deep(.v-card-actions .v-btn) {
  color: white;
}
</style>

<style>
.main-form-page,
.main-form-page .v-main__wrap {
  overflow: hidden !important;
  scrollbar-width: none;
}

.main-form-page::-webkit-scrollbar,
.main-form-page .v-main__wrap::-webkit-scrollbar {
  display: none;
}
</style>
