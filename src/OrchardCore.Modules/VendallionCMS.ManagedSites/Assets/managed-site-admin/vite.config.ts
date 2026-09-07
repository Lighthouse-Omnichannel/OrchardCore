import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default defineConfig({
    plugins: [react()],
    build: {
        outDir: path.resolve(__dirname, '../../wwwroot/Scripts/managed-site-admin/'),
        emptyOutDir: true,
        copyPublicDir: false,
        rollupOptions: {
            input: path.resolve(__dirname, 'src/main.tsx'),
            output: {
                format: 'es',
                entryFileNames: 'managed-site-admin.js',
                assetFileNames: 'managed-site-admin.[ext]',
                manualChunks: undefined,
            },
        },
    },
});
