import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

/* ── Polyfills for jsdom ── */

// matchMedia (MUI responsive breakpoints)
Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

// scrollTo
window.scrollTo = () => {};
Element.prototype.scrollTo = () => {};

// crypto.randomUUID
if (typeof globalThis.crypto?.randomUUID !== "function") {
  Object.defineProperty(globalThis, "crypto", {
    value: {
      ...globalThis.crypto,
      randomUUID: () => "00000000-0000-4000-8000-000000000000",
    },
  });
}

// confirm
window.confirm = () => true;

/* ── Cleanup ── */

afterEach(() => {
  cleanup();
});
