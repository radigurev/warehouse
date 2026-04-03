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
import { useUsersView } from '@features/auth/composables/useUsersView';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import UserFormDialog from '@features/auth/components/organisms/UserFormDialog.vue';
import UserRolesDialog from '@features/auth/components/organisms/UserRolesDialog.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const {
  t,
  layout,
  loading,
  selectedUser,
  showFormDialog,
  showRolesDialog,
  showDeactivateDialog,
  deactivating,
  showPasswordDialog,
  generatedPassword,
  columnFilters,
  filteredItems,
  headers,
  loadUsers,
  formatDate,
  handleCreate,
  handleEdit,
  handleRoles,
  openResetPasswordDialog,
  copyPassword,
  openDeactivateDialog,
  handleDeactivate,
} = useUsersView();
</script>
