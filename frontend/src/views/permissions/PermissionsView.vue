<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="showFormDialog = true">
        {{ t('permissions.create') }}
      </v-btn>
    </div>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="filteredItems"
        :loading="loading"
        :density="layout.vuetifyDensity"
        :items-per-page="25"
        hover
      >
        <template #header.resource="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.resource" column-key="resource" />
          </div>
        </template>

        <template #header.action="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.action" column-key="action" />
          </div>
        </template>
      </v-data-table>
    </v-card>

    <PermissionFormDialog v-model="showFormDialog" @saved="loadPermissions" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@/stores/layout';
import { useNotificationStore } from '@/stores/notification';
import { useColumnFilters } from '@/composables/useColumnFilters';
import { getPermissions } from '@/api/permissions';
import type { PermissionDto } from '@/types/permission';
import ColumnFilter from '@/components/molecules/ColumnFilter.vue';
import PermissionFormDialog from '@/components/organisms/PermissionFormDialog.vue';

const { t } = useI18n();
const layout = useLayoutStore();
const notification = useNotificationStore();

const permissions = ref<PermissionDto[]>([]);
const loading = ref(false);
const showFormDialog = ref(false);

const { columnFilters, filteredItems } = useColumnFilters(permissions, ['resource', 'action']);

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
