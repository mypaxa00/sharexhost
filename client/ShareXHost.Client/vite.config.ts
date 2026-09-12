import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/me': {
        target: 'http://localhost:5128'
      },
      '/dev-token': {
        target: 'http://localhost:5128'
      },
      '/antiforgery/token': {
        target: 'http://localhost:5128'
      },
      '/files': {
        target: 'http://localhost:5128'
      }
    }
  }
})
