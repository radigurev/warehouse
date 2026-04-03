<template>
  <FormWrapper v-model="visible" :mode="mode" max-width="600">
    <v-card-title class="text-h6">
      {{ t('users.rolesDialog.title', { name: userName }) }}
    </v-card-title>

    <v-card-text>
      <v-progress-linear v-if="loading" indeterminate class="mb-4" />

      <h3 class="text-subtitle-1 mb-2">{{ t('users.rolesDialog.assigned') }}</h3>
      <v-chip-group v-if="assignedRoles.length > 0" class="mb-4">
        <v-chip
          v-for="role in assignedRoles"
          :key="role.id"
          closable
          color="primary"
          @click:close="handleRemoveRole(role.id)"
        >
          {{ role.name }}
        </v-chip>
      </v-chip-group>
      <p v-else class="text-body-2 text-grey mb-4">{{ t('users.rolesDialog.noRolesAssigned') }}</p>

      <v-divider class="mb-4" />

      <h3 class="text-subtitle-1 mb-2">{{ t('users.rolesDialog.available') }}</h3>
      <v-chip-group class="mb-2">
        <v-chip
          v-for="role in availableRoles"
          :key="role.id"
          variant="outlined"
          color="primary"
          @click="handleAssignRole(role.id)"
          append-icon="mdi-plus"
        >
          {{ role.name }}
        </v-chip>
      </v-chip-group>
    </v-card-text>

    <v-card-actions>
      <v-spacer />
      <v-btn variant="text" @click="cancel">{{ t('common.close') }}</v-btn>
    </v-card-actions>
  </FormWrapper>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotificationStore } from '@/stores/notification';
import { getUserRoles, assignRoles, removeRole } from '@/api/users';
import { getRoles } from '@/api/roles';
import type { RoleDto } from '@/types/role';
import FormWrapper from '@/components/molecules/FormWrapper.vue';

const { t } = useI18n();
const notification = useNotificationStore();

const visible = defineModel<boolean>({ required: true });

const props = defineProps<{
  userId: number;
  userName: string;
  mode?: 'dialog' | 'page';
}>();

const emit = defineEmits<{
  cancelled: [];
}>();

const loading = ref(false);
const assignedRoles = ref<RoleDto[]>([]);
const allRoles = ref<RoleDto[]>([]);

const availableRoles = computed(() =>
  allRoles.value.filter((r) => !assignedRoles.value.some((a) => a.id === r.id)),
);

watch(visible, async (val) => {
  if (val) {
    await loadRoles();
  }
});

async function loadRoles(): Promise<void> {
  loading.value = true;
  try {
    const [userRoles, roles] = await Promise.all([
      getUserRoles(props.userId),
      getRoles(),
    ]);
    assignedRoles.value = userRoles;
    allRoles.value = roles;
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    loading.value = false;
  }
}

async function handleAssignRole(roleId: number): Promise<void> {
  try {
    await assignRoles(props.userId, { roleIds: [roleId] });
    await loadRoles();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  }
}

async function handleRemoveRole(roleId: number): Promise<void> {
  try {
    await removeRole(props.userId, roleId);
    await loadRoles();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  }
}

function cancel(): void {
  visible.value = false;
  emit('cancelled');
}
</script>
