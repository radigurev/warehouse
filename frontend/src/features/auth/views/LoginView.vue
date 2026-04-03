<template>
  <v-app>
    <v-main class="d-flex align-center justify-center" style="min-height: 100vh; background: #f5f5f5">
      <v-card width="420" class="pa-6" elevation="8">
        <div class="text-center mb-6">
          <v-icon icon="mdi-warehouse" size="64" color="primary" />
          <h1 class="text-h5 mt-3">{{ vm.t('login.title') }}</h1>
          <p class="text-body-2 text-grey mt-1">{{ vm.t('login.subtitle') }}</p>
        </div>

        <v-alert
          v-if="vm.errorMessage"
          type="error"
          variant="tonal"
          class="mb-4"
          density="compact"
        >
          {{ vm.errorMessage }}
        </v-alert>

        <v-form :ref="(el: any) => vm.formRef = el" @submit.prevent="vm.handleLogin" :disabled="vm.loading">
          <v-text-field
            v-model="vm.form.username"
            :label="vm.t('login.username')"
            prepend-inner-icon="mdi-account"
            :rules="[vm.rules.required]"
            autofocus
            class="mb-2"
          />

          <v-text-field
            v-model="vm.form.password"
            :label="vm.t('login.password')"
            prepend-inner-icon="mdi-lock"
            :type="vm.showPassword ? 'text' : 'password'"
            :append-inner-icon="vm.showPassword ? 'mdi-eye-off' : 'mdi-eye'"
            :rules="[vm.rules.required]"
            @click:append-inner="vm.showPassword = !vm.showPassword"
            class="mb-4"
          />

          <v-btn
            type="submit"
            color="primary"
            block
            size="large"
            :loading="vm.loading"
          >
            {{ vm.loading ? vm.t('login.loading') : vm.t('login.submit') }}
          </v-btn>
        </v-form>

        <div class="text-center mt-4">
          <v-btn
            variant="text"
            size="small"
            :prepend-icon="vm.currentLocale === 'en' ? 'mdi-alpha-e-box' : 'mdi-alpha-b-box'"
            @click="vm.toggleLocale"
          >
            {{ vm.currentLocale === 'en' ? 'Български' : 'English' }}
          </v-btn>
        </div>
      </v-card>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useLoginView } from '@features/auth/composables/useLoginView';

const vm = reactive(useLoginView());
</script>
