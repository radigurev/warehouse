<template>
  <div>
    <div class="d-flex align-center mb-2">
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-plus" @click="vm.handleCreate">
        {{ vm.t('users.create') }}
      </v-btn>
    </div>

    <v-card class="view-list-card">
      <v-data-table-server
        :headers="vm.headers"
        :items="vm.filteredItems"
        :items-length="vm.totalCount"
        :loading="vm.loading"
        :density="vm.layout.vuetifyDensity"
        :page="vm.page"
        :items-per-page="vm.pageSize"
        :items-per-page-options="[10, 25, 50, 100]"
        fixed-header
        hover
        @update:page="vm.handlePageChange"
        @update:items-per-page="vm.handlePageSizeChange"
      >
        <template #header.username="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.username" column-key="username" />
          </div>
        </template>

        <template #header.email="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.email" column-key="email" />
          </div>
        </template>

        <template #header.firstName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.firstName" column-key="firstName" />
          </div>
        </template>

        <template #header.lastName="{ column }">
          <div class="d-inline-flex align-center">
            {{ column.title }}
            <ColumnFilter v-model="vm.columnFilters.lastName" column-key="lastName" />
          </div>
        </template>

        <template #item.isActive="{ item }">
          <StatusChip :active="item.isActive" />
        </template>

        <template #item.createdAt="{ item }">
          {{ vm.formatDate(item.createdAt) }}
        </template>

        <template #item.actions="{ item }">
          <ActionChip :label="vm.t('common.edit')" icon="mdi-pencil" color="primary" :compact="vm.layout.isCompact" @click="vm.handleEdit(item)" />
          <ActionChip :label="vm.t('users.resetPassword')" icon="mdi-lock-reset" color="info" :compact="vm.layout.isCompact" @click="vm.openResetPasswordDialog(item)" />
          <ActionChip :label="vm.t('users.manageRoles')" icon="mdi-shield-account" color="accent" :compact="vm.layout.isCompact" @click="vm.handleRoles(item)" />
          <ActionChip v-if="item.isActive" :label="vm.t('users.deactivate')" icon="mdi-account-off" color="error" :compact="vm.layout.isCompact" @click="vm.openDeactivateDialog(item)" />
        </template>

        <template #tfoot>
          <tr>
            <td :colspan="vm.headers.length" class="text-center text-caption text-medium-emphasis py-1">
              {{ vm.t('common.pageInfo', { page: vm.page, pages: vm.totalPages, total: vm.totalCount }) }}
            </td>
          </tr>
        </template>
      </v-data-table-server>
    </v-card>

    <UserFormDialog v-model="vm.showFormDialog" :user="vm.selectedUser" @saved="vm.loadUsers" />
    <UserRolesDialog v-model="vm.showRolesDialog" :user-id="vm.selectedUser?.id ?? 0" :user-name="vm.selectedUser?.username ?? ''" />
    <ConfirmDialog
      v-model="vm.showDeactivateDialog"
      :title="vm.t('users.deactivate')"
      :message="vm.t('users.deactivateConfirm', { name: vm.selectedUser?.username })"
      :confirm-text="vm.t('users.deactivate')"
      color="error"
      icon="mdi-account-off"
      :loading="vm.deactivating"
      @confirm="vm.handleDeactivate"
    />

    <v-dialog v-model="vm.showPasswordDialog" max-width="450" persistent>
      <v-card>
        <div class="d-flex align-center pa-3" style="background: #334155; color: white">
          <v-icon icon="mdi-shield-key" class="mr-2" />
          <span class="text-subtitle-1 font-weight-medium">{{ vm.t('users.generatedPassword') }}</span>
          <v-spacer />
          <v-btn icon="mdi-close" variant="text" size="small" color="white" @click="vm.showPasswordDialog = false" />
        </div>
        <v-card-text>
          <v-alert type="warning" variant="tonal" density="compact" class="mb-4">
            {{ vm.t('users.generatedPasswordHint') }}
          </v-alert>
          <v-text-field
            :model-value="vm.generatedPassword"
            :label="vm.t('users.form.password')"
            prepend-inner-icon="mdi-lock"
            append-inner-icon="mdi-content-copy"
            readonly
            @click:append-inner="vm.copyPassword"
          />
        </v-card-text>
        <v-card-actions style="background: #334155">
          <v-spacer />
          <v-btn color="white" variant="flat" @click="vm.showPasswordDialog = false">{{ vm.t('common.close') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useUsersView } from '@features/auth/composables/useUsersView';
import StatusChip from '@shared/components/atoms/StatusChip.vue';
import ActionChip from '@shared/components/atoms/ActionChip.vue';
import ColumnFilter from '@shared/components/molecules/ColumnFilter.vue';
import UserFormDialog from '@features/auth/components/organisms/UserFormDialog.vue';
import UserRolesDialog from '@features/auth/components/organisms/UserRolesDialog.vue';
import ConfirmDialog from '@shared/components/molecules/ConfirmDialog.vue';

const vm = reactive(useUsersView());
</script>
