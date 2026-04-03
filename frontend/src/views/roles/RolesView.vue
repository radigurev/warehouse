<template>
  <div>
    <div class="d-flex align-center mb-4">
      <h1 class="text-h4">{{ t('roles.title') }}</h1>
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateDialog">
        {{ t('roles.create') }}
      </v-btn>
    </div>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="roles"
        :loading="loading"
        :density="layout.vuetifyDensity"
        :items-per-page="25"
        hover
      >
        <template #item.isSystem="{ item }">
          <v-chip v-if="item.isSystem" color="warning" size="small" variant="flat">
            {{ t('roles.system') }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditDialog(item)" :title="t('common.edit')" />
          <v-btn icon="mdi-key" size="small" variant="text" @click="openPermissionsDialog(item)" :title="t('roles.managePermissions')" />
          <v-btn
            v-if="!item.isSystem"
            icon="mdi-delete"
            size="small"
            variant="text"
            color="error"
            @click="openDeleteDialog(item)"
            :title="t('roles.delete')"
          />
        </template>
      </v-data-table>
    </v-card>

    <RoleFormDialog
      v-model="showFormDialog"
      :role="selectedRole"
      @saved="loadRoles"
    />

    <RolePermissionsDialog
      v-model="showPermissionsDialog"
      :role-id="selectedRole?.id ?? 0"
      :role-name="selectedRole?.name ?? ''"
    />

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
import { useLayoutStore } from '@/stores/layout';
import { useNotificationStore } from '@/stores/notification';
import { getRoles, deleteRole } from '@/api/roles';
import type { RoleDto } from '@/types/role';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@/types/api';
import RoleFormDialog from '@/components/roles/RoleFormDialog.vue';
import RolePermissionsDialog from '@/components/roles/RolePermissionsDialog.vue';
import ConfirmDialog from '@/components/common/ConfirmDialog.vue';

const { t } = useI18n();
const layout = useLayoutStore();
const notification = useNotificationStore();

const roles = ref<RoleDto[]>([]);
const loading = ref(false);
const selectedRole = ref<RoleDto | null>(null);
const showFormDialog = ref(false);
const showPermissionsDialog = ref(false);
const showDeleteDialog = ref(false);
const deleting = ref(false);

const headers = computed(() => [
  { title: t('roles.columns.name'), key: 'name', sortable: true },
  { title: t('roles.columns.description'), key: 'description', sortable: false },
  { title: t('roles.columns.isSystem'), key: 'isSystem', sortable: true },
  { title: t('roles.columns.actions'), key: 'actions', sortable: false, align: 'end' as const },
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

function openCreateDialog(): void {
  selectedRole.value = null;
  showFormDialog.value = true;
}

function openEditDialog(role: RoleDto): void {
  selectedRole.value = role;
  showFormDialog.value = true;
}

function openPermissionsDialog(role: RoleDto): void {
  selectedRole.value = role;
  showPermissionsDialog.value = true;
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
