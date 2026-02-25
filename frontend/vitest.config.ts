import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config";

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      globals: true,
      environment: "jsdom",
      css: false,
      include: ["src/{hooks,auth,lib,utils,test}/**/*.test.{ts,tsx}"],
      setupFiles: ["./src/test/setupTests.ts"],
      restoreMocks: true,
      mockReset: true,
      clearMocks: true,
    },
  }),
);
