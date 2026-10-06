import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// During development, /api calls are proxied to the ASP.NET Core API.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5000', changeOrigin: true }
    }
  }
});
