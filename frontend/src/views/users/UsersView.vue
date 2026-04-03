<template>
  <div>
    <div class="d-flex align-center mb-4">
      <h1 class="text-h4">{{ t('users.title') }}</h1>
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateDialog">
        {{ t('users.create') }}
      </v-btn>
    </div>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="users"
        :loading="loading"
        :density="layout.vuetifyDensity"
        :items-per-page="25"
        hover
      >
        <template #item.isActive="{ item }">
          <v-chip :color="item.isActive ? 'success' : 'grey'" size="small" variant="flat">
            {{ item.isActive ? t('common.active') : t('common.inactive') }}
          </v-chip>
        </template>

        <template #item.createdAt="{ item }">
          {{ formatDate(item.createdAt) }}
        </template>

        <template #item.actions="{ item }">
          <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditDialog(item)" :title="t('common.edit')" />
          <v-btn icon="mdi-lock-reset" size="small" variant="text" @click="openPasswordDialog(item)" :title="t('users.changePassword')" />
          <v-btn icon="mdi-shield-account" size="small" variant="text" @click="openRolesDialog(item)" :title="t('users.manageRoles')" />
          <v-btn
            v-if="item.isActive"
            icon="mdi-account-off"
            size="small"
            variant="text"
            color="error"
            @click="openDeactivateDialog(item)"
            :title="t('users.deactivate')"
          />
        </template>
      </v-data-table>
    </v-card>

    <UserFormDialog
      v-model="showFormDialog"
      :user="selectedUser"
      @saved="loadUsers"
    />

    <ChangePasswordDialog
      v-model="showPasswordDialog"
      :user-id="selectedUser?.id ?? 0"
    />

    <UserRolesDialog
      v-model="showRolesDialog"
      :user-id="selectedUser?.id ?? 0"
      :user-name="selectedUser?.username ?? ''"
    />

    <ConfirmDialog
      v-model="showDeactivateDialog"
      :title="t('users.deactivate')"
      :message="t('users.deactivateConfirm', { name: selectedUser?.username })"
      :confirm-text="t('users.deactivate')"
      color="error"
      icon="mdi-account-off"
      :loading="deactivating"
      @confirm="handleDeactivate"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useLayoutStore } from '@/stores/layout';
import { useAuthStore } from '@/stores/auth';
import { useNotificationStore } from '@/stores/notification';
import { getUsers, deactivateUser } from '@/api/users';
import type { UserDto } from '@/types/user';
import UserFormDialog from '@/components/users/UserFormDialog.vue';
import ChangePasswordDialog from '@/components/users/ChangePasswordDialog.vue';
import UserRolesDialog from '@/components/users/UserRolesDialog.vue';
import ConfirmDialog from '@/components/common/ConfirmDialog.vue';

const { t, locale } = useI18n();
const layout = useLayoutStore();
const auth = useAuthStore();
const notification = useNotificationStore();

const users = ref<UserDto[]>([]);
const loading = ref(false);
const selectedUser = ref<UserDto | null>(null);
const showFormDialog = ref(false);
const showPasswordDialog = ref(false);
const showRolesDialog = ref(false);
const showDeactivateDialog = ref(false);
const deactivating = ref(false);

const headers = computed(() => [
  { title: t('users.columns.username'), key: 'username', sortable: true },
  { title: t('users.columns.email'), key: 'email', sortable: true },
  { title: t('users.columns.firstName'), key: 'firstName', sortable: true },
  { title: t('users.columns.lastName'), key: 'lastName', sortable: true },
  { title: t('users.columns.isActive'), key: 'isActive', sortable: true },
  { title: t('users.columns.createdAt'), key: 'createdAt', sortable: true },
  { title: t('users.columns.actions'), key: 'actions', sortable: false, align: 'end' as const },
]);

onMounted(() => loadUsers());

async function loadUsers(): Promise<void> {
  loading.value = true;
  try {
    users.value = await getUsers();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    loading.value = false;
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(locale.value === 'bg' ? 'bg-BG' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function openCreateDialog(): void {
  selectedUser.value = null;
  showFormDialog.value = true;
}

function openEditDialog(user: UserDto): void {
  selectedUser.value = user;
  showFormDialog.value = true;
}

function openPasswordDialog(user: UserDto): void {
  selectedUser.value = user;
  showPasswordDialog.value = true;
}

function openRolesDialog(user: UserDto): void {
  selectedUser.value = user;
  showRolesDialog.value = true;
}

function openDeactivateDialog(user: UserDto): void {
  if (user.username === auth.username) {
    notification.warning(t('users.deactivateSelfWarning'));
    return;
  }
  selectedUser.value = user;
  showDeactivateDialog.value = true;
}

async function handleDeactivate(): Promise<void> {
  if (!selectedUser.value) return;
  deactivating.value = true;
  try {
    await deactivateUser(selectedUser.value.id);
    notification.success(t('users.deactivate') + ' ✓');
    showDeactivateDialog.value = false;
    await loadUsers();
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  } finally {
    deactivating.value = false;
  }
}
</script>
