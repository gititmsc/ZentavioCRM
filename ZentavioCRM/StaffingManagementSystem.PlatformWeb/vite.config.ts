import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

// Port 5174 — deliberately one above the tenant-facing app's 5173, so both can run side by
// side in local dev without a port clash.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5174,
  },
});
