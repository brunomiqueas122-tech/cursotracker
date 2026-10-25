/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig(({ mode }) => ({
  plugins: [react()],
  // GitHub Pages serve o app em /cursotracker/
  base: mode === 'pages' ? '/cursotracker/' : '/',
  server: {
    proxy: {
      '/api': 'http://localhost:5080',
    },
    fs: {
      // seed-courses.json fica na raiz do repositório
      allow: ['..'],
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test-setup.ts'],
  },
}))
