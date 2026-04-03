<template>
  <v-app>
    <v-main class="d-flex align-center justify-center" style="min-height: 100vh; background: #f5f5f5">
      <v-card width="420" class="pa-6" elevation="8">
        <div class="text-center mb-6">
          <v-icon icon="mdi-warehouse" size="64" color="primary" />
          <h1 class="text-h5 mt-3">{{ t('login.title') }}</h1>
          <p class="text-body-2 text-grey mt-1">{{ t('login.subtitle') }}</p>
        </div>

        <v-alert
          v-if="errorMessage"
          type="error"
          variant="tonal"
          class="mb-4"
          density="compact"
        >
          {{ errorMessage }}
        </v-alert>

        <v-form ref="formRef" @submit.prevent="handleLogin" :disabled="loading">
          <v-text-field
            v-model="form.username"
            :label="t('login.username')"
            prepend-inner-icon="mdi-account"
            :rules="[rules.required]"
            autofocus
            class="mb-2"
          />

          <v-text-field
            v-model="form.password"
            :label="t('login.password')"
            prepend-inner-icon="mdi-lock"
            :type="showPassword ? 'text' : 'password'"
            :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
            :rules="[rules.required]"
            @click:append-inner="showPassword = !showPassword"
            class="mb-4"
          />

          <v-btn
            type="submit"
            color="primary"
            block
            size="large"
            :loading="loading"
          >
            {{ loading ? t('login.loading') : t('login.submit') }}
          </v-btn>
        </v-form>

        <div class="text-center mt-4">
          <v-btn
            variant="text"
            size="small"
            :prepend-icon="currentLocale === 'en' ? 'mdi-alpha-e-box' : 'mdi-alpha-b-box'"
            @click="toggleLocale"
          >
            {{ currentLocale === 'en' ? 'Български' : 'English' }}
          </v-btn>
        </div>
      </v-card>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import { useLoginView } from '@features/auth/composables/useLoginView';

const {
  formRef,
  loading,
  showPassword,
  errorMessage,
  form,
  currentLocale,
  rules,
  t,
  toggleLocale,
  handleLogin,
} = useLoginView();
</script>
