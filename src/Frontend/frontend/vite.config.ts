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
      '/api/catalog/items': {
        target: 'http://localhost:7001',
        changeOrigin: true,
      },
      '/api/orders': {
        target: 'http://localhost:7002',
        changeOrigin: true,
      },
      '/api/catalog': {
        target: 'http://localhost:7004',
        changeOrigin: true,
      },
    },
  },
})
