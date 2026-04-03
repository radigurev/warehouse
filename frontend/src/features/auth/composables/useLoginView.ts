import { ref, reactive, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@shared/stores/auth';
import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

export function useLoginView() {
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

  return {
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
  };
}
