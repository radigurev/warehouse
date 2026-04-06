import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vuetify from 'vite-plugin-vuetify';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [
    vue(),
    vuetify({ autoImport: true }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@features': fileURLToPath(new URL('./src/features', import.meta.url)),
      '@shared': fileURLToPath(new URL('./src/shared', import.meta.url)),
      '@app': fileURLToPath(new URL('./src/app', import.meta.url)),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api/v1/customers': {
        target: 'http://localhost:5002',
        changeOrigin: true,
      },
      '/api/v1/customer-categories': {
        target: 'http://localhost:5002',
        changeOrigin: true,
      },
      '/api/v1/products': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/product-categories': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/units-of-measure': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/product-accessories': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/product-substitutes': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/bom': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/warehouses': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/zones': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/storage-locations': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/stock-levels': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/stock-movements': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/batches': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/adjustments': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/transfers': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/v1/stocktake': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api': {
        target: 'http://localhost:5001',
        changeOrigin: true,
      },
    },
  },
});
