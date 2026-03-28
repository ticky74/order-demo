import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 3000,
    proxy: {
      '/api/orders': {
        target: 'https://localhost:7002',
        changeOrigin: true,
        secure: false,
      },
      '/api/catalog': {
        target: 'https://localhost:7004',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
