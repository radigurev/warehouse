<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="handleCreate">
        {{ t('users.create') }}
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
        <template #header.username="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.username" column-key="username" />
          </div>
        </template>

        <template #header.email="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.email" column-key="email" />
          </div>
        </template>

        <template #header.firstName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.firstName" column-key="firstName" />
          </div>
        </template>

        <template #header.lastName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="columnFilters.lastName" column-key="lastName" />
          </div>
        </template>

        <template #item.isActive="{ item }">
          <StatusChip :active="item.isActive" />
        </template>

        <template #item.createdAt="{ item }">
          {{ formatDate(item.createdAt) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="t('common.edit')" icon="mdi-pencil" color="primary" :compact="layout.isCompact" @click="handleEdit(item)" />
          <ActionChip :label="t('users.resetPassword')" icon="mdi-lock-reset" color="info" :compact="layout.isCompact" @click="openResetPasswordDialog(item)" />
          <ActionChip :label="t('users.manageRoles')" icon="mdi-shield-account" color="accent" :compact="layout.isCompact" @click="handleRoles(item)" />
          <ActionChip v-if="item.isActive" :label="t('users.deactivate')" icon="mdi-account-off" color="error" :compact="layout.isCompact" @click="openDeactivateDialog(item)" />
        </template>
      </v-data-table>
    </v-card>

    <UserFormDialog v-model="showFormDialog" :user="selectedUser" @saved="loadUsers" />
    <UserRolesDialog v-model="showRolesDialog" :user-id="selectedUser?.id ?? 0" :user-name="selectedUser?.username ?? ''" />
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

    <v-dialog v-model="showPasswordDialog" max-width="450" persistent>
      <v-card>
        <div class="d-flex align-center pa-3" style="background: #334155; color: white">
          <v-icon icon="mdi-shield-key" class="mr-2" />
          <span class="text-subtitle-1 font-weight-medium">{{ t('users.generatedPassword') }}</span>
          <v-spacer />
          <v-btn icon="mdi-close" variant="text" size="small" color="white" @click="showPasswordDialog = false" />
        </div>
        <v-card-text>
          <v-alert type="warning" variant="tonal" density="compact" class="mb-4">
            {{ t('users.generatedPasswordHint') }}
          </v-alert>
          <v-text-field
            :model-value="generatedPassword"
            :label="t('users.form.password')"
            prepend-inner-icon="mdi-lock"
            append-inner-icon="mdi-content-copy"
            readonly
            @click:append-inner="copyPassword"
          />
        </v-card-text>
        <v-card-actions style="background: #334155">
          <v-spacer />
          <v-btn color="white" variant="flat" @click="showPasswordDialog = false">{{ t('common.close') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useLayoutStore } from '@/stores/layout';
import { useAuthStore } from '@/stores/auth';
import { useNotificationStore } from '@/stores/notification';
import { useColumnFilters } from '@/composables/useColumnFilters';
import { getUsers, deactivateUser, resetPassword } from '@/api/users';
import type { UserDto } from '@/types/user';
import StatusChip from '@/components/atoms/StatusChip.vue';
import ActionChip from '@/components/atoms/ActionChip.vue';
import ColumnFilter from '@/components/molecules/ColumnFilter.vue';
import UserFormDialog from '@/components/organisms/UserFormDialog.vue';
import UserRolesDialog from '@/components/organisms/UserRolesDialog.vue';
import ConfirmDialog from '@/components/molecules/ConfirmDialog.vue';

const { t, locale } = useI18n();
const router = useRouter();
const layout = useLayoutStore();
const auth = useAuthStore();
const notification = useNotificationStore();

const users = ref<UserDto[]>([]);
const loading = ref(false);
const selectedUser = ref<UserDto | null>(null);
const showFormDialog = ref(false);
const showRolesDialog = ref(false);
const showDeactivateDialog = ref(false);
const deactivating = ref(false);
const showPasswordDialog = ref(false);
const generatedPassword = ref('');

const { columnFilters, filteredItems } = useColumnFilters(users, ['username', 'email', 'firstName', 'lastName']);

const headers = computed(() => [
  { title: t('users.columns.username'), key: 'username', sortable: true },
  { title: t('users.columns.email'), key: 'email', sortable: true },
  { title: t('users.columns.firstName'), key: 'firstName', sortable: true },
  { title: t('users.columns.lastName'), key: 'lastName', sortable: true },
  { title: t('users.columns.isActive'), key: 'isActive', sortable: true },
  { title: t('users.columns.createdAt'), key: 'createdAt', sortable: true },
  { title: t('users.columns.actions'), key: 'actions', sortable: false, align: 'end' as const, minWidth: '340px' },
]);

onMounted(() => loadUsers());

async function loadUsers(): Promise<void> {
  loading.value = true;
  try {
    const response = await getUsers();
    users.value = response.items;
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

function handleCreate(): void {
  if (layout.isPageMode) {
    router.push({ name: 'user-create' });
  } else {
    selectedUser.value = null;
    showFormDialog.value = true;
  }
}

function handleEdit(user: UserDto): void {
  if (layout.isPageMode) {
    router.push({ name: 'user-edit', params: { id: user.id } });
  } else {
    selectedUser.value = user;
    showFormDialog.value = true;
  }
}

function handleRoles(user: UserDto): void {
  if (layout.isPageMode) {
    router.push({ name: 'user-roles', params: { id: user.id } });
  } else {
    selectedUser.value = user;
    showRolesDialog.value = true;
  }
}

async function openResetPasswordDialog(user: UserDto): Promise<void> {
  try {
    const response = await resetPassword(user.id);
    generatedPassword.value = response.generatedPassword;
    showPasswordDialog.value = true;
  } catch {
    notification.error(t('errors.UNEXPECTED_ERROR'));
  }
}

function copyPassword(): void {
  navigator.clipboard.writeText(generatedPassword.value);
  notification.success(t('users.passwordCopied'));
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
