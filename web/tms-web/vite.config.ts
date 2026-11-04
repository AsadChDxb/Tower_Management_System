import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The dev server proxies /api and /health to the deployed backend on Azure App Service, so those
// paths stay same-origin while developing (the SPA itself calls the API at API_BASE_URL - see
// src/api/client.ts). Point the proxy at a local API with:
//   $env:VITE_API_TARGET='http://localhost:5080'; npm run dev
const apiTarget =
  process.env.VITE_API_TARGET ??
  'https://weatherdemoapp-h0a4ggb7dzh4href.indiasouthcentral-01.azurewebsites.net';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true
      },
      '/health': {
        target: apiTarget,
        changeOrigin: true
      }
    }
  }
});
