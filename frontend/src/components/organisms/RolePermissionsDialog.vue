<template>
  <v-dialog v-model="visible" max-width="700" persistent>
    <v-card>
      <v-card-title class="text-h6">
        {{ t('roles.permissionsDialog.title', { name: roleName }) }}
      </v-card-title>

      <v-card-text>
        <v-progress-linear v-if="loading" indeterminate class="mb-4" />

        <h3 class="text-subtitle-1 mb-2">{{ t('roles.permissionsDialog.assigned') }}</h3>
        <v-chip-group v-if="assignedPermissions.length > 0" class="mb-4">
          <v-chip
            v-for="perm in assignedPermissions"
            :key="perm.id"
            closable
            color="primary"
            @click:close="handleRemovePermission(perm.id)"
          >
            {{ perm.resource }}:{{ perm.action }}
          </v-chip>
        </v-chip-group>
        <p v-else class="text-body-2 text-grey mb-4">{{ t('roles.permissionsDialog.noPermissionsAssigned') }}</p>

        <v-divider class="mb-4" />

        <h3 class="text-subtitle-1 mb-2">{{ t('roles.permissionsDialog.available') }}</h3>
        <div v-for="[resource, perms] in groupedAvailable" :key="resource" class="mb-3">
          <div class="text-caption text-grey-darken-1 mb-1">{{ resource }}</div>
          <v-chip-group>
            <v-chip
              v-for="perm in perms"
              :key="perm.id"
              variant="outlined"
              color="primary"
              append-icon="mdi-plus"
              @click="handleAssignPermission(perm.id)"
            >
              {{ perm.action }}
            </v-chip>
          </v-chip-group>
        </div>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="visible = false">{{ t('common.close') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@/stores/notification';
import { getRolePermissions, assignPermissions, removePermission } from '@/api/roles';
import { getPermissions } from '@/api/permissions';
import type { PermissionDto } from '@/types/permission';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  roleId: number;
  roleName: string;
}>();

const loading = ref(false);
const assignedPermissions = ref<PermissionDto[]>([]);
const allPermissions = ref<PermissionDto[]>([]);

const availablePermissions = computed(() =>
  allPermissions.value.filter((p) => !assignedPermissions.value.some((a) => a.id === p.id)),
);

const groupedAvailable = computed(() => {
  const map = new Map<string, PermissionDto[]>();
  for (const perm of availablePermissions.value) {
    const group = map.get(perm.resource) || [];
    group.push(perm);
    map.set(perm.resource, group);
  }
  return Array.from(map.entries()).sort(([a], [b]) => a.localeCompare(b));
});

watch(visible, async (val) => {
  if (val) {
    await loadPermissions();
  }
});

async function loadPermissions(): Promise<void> {
  loading.value = true;
  try {
    const [rolePerms, perms] = await Promise.all([
      getRolePermissions(props.roleId),
      getPermissions(),
    ]);
    assignedPermissions.value = rolePerms;
    allPermissions.value = perms;
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    loading.value = false;
  }
}

async function handleAssignPermission(permissionId: number): Promise<void> {
  try {
    await assignPermissions(props.roleId, { permissionIds: [permissionId] });
    await loadPermissions();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  }
}

async function handleRemovePermission(permissionId: number): Promise<void> {
  try {
    await removePermission(props.roleId, permissionId);
    await loadPermissions();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  }
}
</script>
