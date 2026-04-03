<template>
  <div>
    <v-progress-linear v-if="vm.loading" indeterminate class="mb-4" />

    <v-row v-if="vm.profile">
      <v-col cols="12" md="6">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon icon="mdi-account-circle" class="mr-2" color="primary" />
            {{ vm.t('settings.profile') }}
          </v-card-title>
          <v-card-text>
            <v-list density="compact">
              <v-list-item :title="vm.t('users.columns.username')" :subtitle="vm.profile.username" prepend-icon="mdi-account" />
              <v-list-item :title="vm.t('users.columns.email')" :subtitle="vm.profile.email" prepend-icon="mdi-email" />
              <v-list-item :title="vm.t('users.columns.firstName')" :subtitle="vm.profile.firstName" prepend-icon="mdi-badge-account-horizontal" />
              <v-list-item :title="vm.t('users.columns.lastName')" :subtitle="vm.profile.lastName" prepend-icon="mdi-badge-account" />
            </v-list>
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn color="primary" variant="flat" prepend-icon="mdi-pencil" @click="vm.handleEditProfile">
              {{ vm.t('settings.editProfile') }}
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon icon="mdi-lock" class="mr-2" color="primary" />
            {{ vm.t('settings.security') }}
          </v-card-title>
          <v-card-text>
            <p class="text-body-2 text-medium-emphasis">
              {{ vm.t('settings.passwordHint') }}
            </p>
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn color="primary" variant="flat" prepend-icon="mdi-lock-reset" @click="vm.handleChangePassword">
              {{ vm.t('users.changePassword') }}
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <ProfileEditDialog
      v-model="vm.showProfileDialog"
      :user="vm.profile"
      :user-id="vm.auth.userId ?? 0"
      @saved="vm.loadProfile"
    />

    <ChangePasswordDialog
      v-model="vm.showPasswordDialog"
      :user-id="vm.auth.userId ?? 0"
      @saved="vm.onPasswordChanged"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useSettingsView } from '@features/auth/composables/useSettingsView';
import ProfileEditDialog from '@features/auth/components/organisms/ProfileEditDialog.vue';
import ChangePasswordDialog from '@features/auth/components/organisms/ChangePasswordDialog.vue';

const vm = reactive(useSettingsView());
</script>
