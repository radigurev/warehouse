import { createApp } from 'vue';
import { createPinia } from 'pinia';
import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';
import 'vuetify/styles';
import '@mdi/font/css/materialdesignicons.css';

import App from './App.vue';
import router from './router';
import i18n from './i18n';

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'light',
    themes: {
      light: {
        colors: {
          primary: '#1565C0',
          secondary: '#455A64',
          accent: '#0288D1',
          error: '#D32F2F',
          warning: '#F57C00',
          info: '#0288D1',
          success: '#388E3C',
        },
      },
    },
  },
  defaults: {
    VTextField: {
      variant: 'outlined',
    },
    VSelect: {
      variant: 'outlined',
    },
    VAutocomplete: {
      variant: 'outlined',
    },
  },
});

const app = createApp(App);
app.use(createPinia());
app.use(router);
app.use(vuetify);
app.use(i18n);
app.mount('#app');
