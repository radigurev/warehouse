<template>
  <div>
    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <v-row v-if="profile">
      <v-col cols="12" md="6">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon icon="mdi-account-circle" class="mr-2" color="primary" />
            {{ t('settings.profile') }}
          </v-card-title>
          <v-card-text>
            <v-list density="compact">
              <v-list-item :title="t('users.columns.username')" :subtitle="profile.username" prepend-icon="mdi-account" />
              <v-list-item :title="t('users.columns.email')" :subtitle="profile.email" prepend-icon="mdi-email" />
              <v-list-item :title="t('users.columns.firstName')" :subtitle="profile.firstName" prepend-icon="mdi-badge-account-horizontal" />
              <v-list-item :title="t('users.columns.lastName')" :subtitle="profile.lastName" prepend-icon="mdi-badge-account" />
            </v-list>
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn color="primary" variant="flat" prepend-icon="mdi-pencil" @click="handleEditProfile">
              {{ t('settings.editProfile') }}
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon icon="mdi-lock" class="mr-2" color="primary" />
            {{ t('settings.security') }}
          </v-card-title>
          <v-card-text>
            <p class="text-body-2 text-medium-emphasis">
              {{ t('settings.passwordHint') }}
            </p>
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn color="primary" variant="flat" prepend-icon="mdi-lock-reset" @click="handleChangePassword">
              {{ t('users.changePassword') }}
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <ProfileEditDialog
      v-model="showProfileDialog"
      :user="profile"
      :user-id="auth.userId ?? 0"
      @saved="loadProfile"
    />

    <ChangePasswordDialog
      v-model="showPasswordDialog"
      :user-id="auth.userId ?? 0"
      @saved="onPasswordChanged"
    />
  </div>
</template>

<script setup lang="ts">
import { useSettingsView } from '@features/auth/composables/useSettingsView';
import ProfileEditDialog from '@features/auth/components/organisms/ProfileEditDialog.vue';
import ChangePasswordDialog from '@features/auth/components/organisms/ChangePasswordDialog.vue';

const {
  t,
  auth,
  profile,
  loading,
  showProfileDialog,
  showPasswordDialog,
  loadProfile,
  handleEditProfile,
  handleChangePassword,
  onPasswordChanged,
} = useSettingsView();
</script>
