<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="handleCreate">
        {{ t('roles.create') }}
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
        <template #header.name="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.name" column-key="name" />
          </div>
        </template>

        <template #item.isSystem="{ item }">
          <v-chip v-if="item.isSystem" color="warning" size="small" variant="flat">
            {{ t('roles.system') }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="t('common.edit')" icon="mdi-pencil" color="primary" :compact="layout.isCompact" @click="handleEdit(item)" />
          <ActionChip :label="t('roles.managePermissions')" icon="mdi-key" color="accent" :compact="layout.isCompact" @click="handlePermissions(item)" />
          <ActionChip v-if="!item.isSystem" :label="t('common.delete')" icon="mdi-delete" color="error" :compact="layout.isCompact" @click="openDeleteDialog(item)" />
        </template>
      </v-data-table>
    </v-card>

    <RoleFormDialog v-model="showFormDialog" :role="selectedRole" @saved="loadRoles" />
    <RolePermissionsDialog v-model="showPermissionsDialog" :role-id="selectedRole?.id ?? 0" :role-name="selectedRole?.name ?? ''" />
    <ConfirmDialog
      v-model="showDeleteDialog"
      :title="t('roles.delete')"
      :message="t('roles.deleteConfirm', { name: selectedRole?.name })"
      :confirm-text="t('common.delete')"
      color="error"
      icon="mdi-delete"
      :loading="deleting"
      @confirm="handleDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@/stores/layout';
import { useNotificationStore } from '@/stores/notification';
import { useColumnFilters } from '@/composables/useColumnFilters';
import { getRoles, deleteRole } from '@/api/roles';
import type { RoleDto } from '@/types/role';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@/types/api';
import ActionChip from '@/components/atoms/ActionChip.vue';
import ColumnFilter from '@/components/molecules/ColumnFilter.vue';
import RoleFormDialog from '@/components/organisms/RoleFormDialog.vue';
import RolePermissionsDialog from '@/components/organisms/RolePermissionsDialog.vue';
import ConfirmDialog from '@/components/molecules/ConfirmDialog.vue';

const { t } = useI18n();
const router = useRouter();
const layout = useLayoutStore();
const notification = useNotificationStore();

const roles = ref<RoleDto[]>([]);
const loading = ref(false);
const selectedRole = ref<RoleDto | null>(null);
const showFormDialog = ref(false);
const showPermissionsDialog = ref(false);
const showDeleteDialog = ref(false);
const deleting = ref(false);

const { columnFilters, filteredItems } = useColumnFilters(roles, ['name']);

const headers = computed(() => [
  { title: t('roles.columns.name'), key: 'name', sortable: true },
  { title: t('roles.columns.description'), key: 'description', sortable: false },
  { title: t('roles.columns.isSystem'), key: 'isSystem', sortable: true },
  { title: t('roles.columns.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '280px' },
]);

onMounted(() => loadRoles());

async function loadRoles(): Promise<void> {
  loading.value = true;
  try {
    roles.value = await getRoles();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    loading.value = false;
  }
}

function handleCreate(): void {
  if (layout.isPageMode) {
    router.push({ name: 'role-create' });
  } else {
    selectedRole.value = null;
    showFormDialog.value = true;
  }
}

function handleEdit(role: RoleDto): void {
  if (layout.isPageMode) {
    router.push({ name: 'role-edit', params: { id: role.id } });
  } else {
    selectedRole.value = role;
    showFormDialog.value = true;
  }
}

function handlePermissions(role: RoleDto): void {
  if (layout.isPageMode) {
    router.push({ name: 'role-permissions', params: { id: role.id } });
  } else {
    selectedRole.value = role;
    showPermissionsDialog.value = true;
  }
}

function openDeleteDialog(role: RoleDto): void {
  selectedRole.value = role;
  showDeleteDialog.value = true;
}

async function handleDelete(): Promise<void> {
  if (!selectedRole.value) return;
  deleting.value = true;
  try {
    await deleteRole(selectedRole.value.id);
    notification.success(t('roles.delete') + ' ✓');
    showDeleteDialog.value = false;
    await loadRoles();
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode === 'ROLE_IN_USE') {
      notification.error(axiosError.response?.data?.detail || t('errors.ROLE_IN_USE'));
    } else if (errorCode === 'PROTECTED_ROLE') {
      notification.error(t('errors.PROTECTED_ROLE'));
    } else {
      notification.error(t('errors.UNEXPECTED_ERROR'));
    }
  } finally {
    deleting.value = false;
  }
}
</script>
