import { createI18n } from 'vue-i18n';
import en from './locales/en';
import bg from './locales/bg';

const savedLocale = localStorage.getItem('locale') || 'en';

const i18n = createI18n({
  legacy: false,
  locale: savedLocale,
  fallbackLocale: 'en',
  messages: {
    en,
    bg,
  },
});

export default i18n;
