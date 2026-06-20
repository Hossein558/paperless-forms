import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../src/main/resources',
    emptyOutDir: false,
    rollupOptions: {
      output: {
        format: 'iife',
        entryFileNames: 'js/paperless-forms-plugin.js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name?.endsWith('.css')) {
            return 'css/paperless-forms-plugin.css';
          }
          return 'images/[name].[ext]';
        }
      }
    }
  }
})
