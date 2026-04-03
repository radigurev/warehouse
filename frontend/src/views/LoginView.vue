<template>
  <v-app>
    <v-main class="d-flex align-center justify-center" style="min-height: 100vh; background: #f5f5f5">
      <v-card width="420" class="pa-6" elevation="8" rounded="lg">
        <div class="text-center mb-6">
          <v-icon icon="mdi-warehouse" size="64" color="blue-darken-3" />
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
            color="blue-darken-3"
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
import { ref, reactive, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@/types/api';

const { t, locale } = useI18n();
const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const formRef = ref();
const loading = ref(false);
const showPassword = ref(false);
const errorMessage = ref('');

const form = reactive({
  username: '',
  password: '',
});

const currentLocale = computed(() => locale.value);

const rules = {
  required: (value: string) => !!value || t('common.required'),
};

onMounted(() => {
  if (route.query.expired) {
    errorMessage.value = t('login.sessionExpired');
  }
});

function toggleLocale(): void {
  const newLocale = locale.value === 'en' ? 'bg' : 'en';
  locale.value = newLocale;
  localStorage.setItem('locale', newLocale);
}

async function handleLogin(): Promise<void> {
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  loading.value = true;
  errorMessage.value = '';

  try {
    await auth.login({ username: form.username, password: form.password });
    const redirect = route.query.redirect as string;
    router.push(redirect || '/');
  } catch (err) {
    const axiosError = err as AxiosError<ProblemDetails>;
    const errorCode = axiosError.response?.data?.title;
    if (errorCode && t(`errors.${errorCode}`) !== `errors.${errorCode}`) {
      errorMessage.value = t(`errors.${errorCode}`);
    } else {
      errorMessage.value = t('errors.INVALID_CREDENTIALS');
    }
  } finally {
    loading.value = false;
  }
}
</script>
