import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: "jsdom",
    css: false,
    include: ["src/test/**/*.test.{ts,tsx}"],
    setupFiles: ["./src/test/setupTests.ts"],
    restoreMocks: true,
    mockReset: true,
    clearMocks: true,
    coverage: {
      exclude: ["src/test/**"],
    },
  },
});
