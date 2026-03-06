import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  base: "./",
  server: {
    proxy: {
      "/api": {
        target: "https://localhost:5003",
        secure: false,
      },
    },
  },
  build: {
    outDir: "build",
  },
});
