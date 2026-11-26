import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false
      },
      '/traffic-api': {
        target: 'http://localhost:5090',
        changeOrigin: true,
        rewrite: path => path.replace(/^\/traffic-api/, '')
      }
    }
  }
})
