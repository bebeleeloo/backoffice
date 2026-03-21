import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  test: {
    globals: true,
    environment: "jsdom",
    css: false,
    include: ["src/{hooks,auth,lib,utils,test}/**/*.test.{ts,tsx}"],
    setupFiles: ["./src/test/setupTests.ts"],
    restoreMocks: true,
    mockReset: true,
    clearMocks: true,
    coverage: {
      exclude: ["src/test/**", "src/types/**"],
    },
  },
});
