<template>
  <div>
    <div class="d-flex align-center mb-4">
      <h1 class="text-h4">{{ t('permissions.title') }}</h1>
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="showFormDialog = true">
        {{ t('permissions.create') }}
      </v-btn>
    </div>

    <v-card>
      <v-text-field
        v-model="search"
        prepend-inner-icon="mdi-magnify"
        :label="t('common.search')"
        single-line
        hide-details
        class="pa-4"
      />

      <v-data-table
        :headers="headers"
        :items="permissions"
        :loading="loading"
        :density="layout.vuetifyDensity"
        :search="search"
        :items-per-page="25"
        hover
      />
    </v-card>

    <PermissionFormDialog
      v-model="showFormDialog"
      @saved="loadPermissions"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@/stores/layout';
import { useNotificationStore } from '@/stores/notification';
import { getPermissions } from '@/api/permissions';
import type { PermissionDto } from '@/types/permission';
import PermissionFormDialog from '@/components/permissions/PermissionFormDialog.vue';

const { t } = useI18n();
const layout = useLayoutStore();
const notification = useNotificationStore();

const permissions = ref<PermissionDto[]>([]);
const loading = ref(false);
const search = ref('');
const showFormDialog = ref(false);

const headers = computed(() => [
  { title: t('permissions.columns.resource'), key: 'resource', sortable: true },
  { title: t('permissions.columns.action'), key: 'action', sortable: true },
  { title: t('permissions.columns.description'), key: 'description', sortable: false },
]);

onMounted(() => loadPermissions());

async function loadPermissions(): Promise<void> {
  loading.value = true;
  try {
    permissions.value = await getPermissions();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    loading.value = false;
  }
}
</script>
