import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

// In Docker, VITE_BACKEND_URL points at the backend service (http://backend:8080).
// Outside Docker it falls back to localhost so the same config works everywhere.
const backendTarget = process.env.VITE_BACKEND_URL || 'http://localhost:8080';

export default defineConfig({
  plugins: [react()],
  server: {
    watch: {
      usePolling: true, // Necessary for some Docker environments to detect file changes
    },
    host: true, // Required for Docker port mapping
    strictPort: true,
    port: 5173,
    proxy: {
      // Forward API calls + game art to the .NET backend
      '/api': {
        target: backendTarget,
        changeOrigin: true,
      },
      '/assets/game-art': {
        target: backendTarget,
        changeOrigin: true,
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});