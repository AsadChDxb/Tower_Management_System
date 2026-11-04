import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The dev server proxies /api to the .NET API so the browser only ever talks to one origin
// (no CORS surprises locally). In production the SPA is served from the API's wwwroot.
// Point it elsewhere with: $env:VITE_API_TARGET='http://localhost:5081'; npm run dev
const apiTarget = process.env.VITE_API_TARGET ?? 'http://localhost:5080';

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
