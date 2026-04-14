import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    proxy: {
      // 将 /api 请求代理到后端，避免跨域问题
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
})
