import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// In development the React app calls /api and /hubs on its own origin;
// Vite forwards them to the .NET API (VITE_API_PROXY, default http://localhost:5180).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  const target = env.VITE_API_PROXY || 'http://localhost:5180'
  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': { target, changeOrigin: true },
        '/hubs': { target, changeOrigin: true, ws: true },
      },
    },
  }
})
