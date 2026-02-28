import { describe, it, expect } from "vitest";
import { validateRequired, validateEmail } from "./validateFields";

describe("validateRequired", () => {
  it("returns undefined for a valid non-empty string", () => {
    expect(validateRequired("hello")).toBeUndefined();
  });

  it("returns undefined for a string with content and surrounding spaces", () => {
    expect(validateRequired("  hello  ")).toBeUndefined();
  });

  it('returns "Required" for an empty string', () => {
    expect(validateRequired("")).toBe("Required");
  });

  it('returns "Required" for null', () => {
    expect(validateRequired(null)).toBe("Required");
  });

  it('returns "Required" for undefined', () => {
    expect(validateRequired(undefined)).toBe("Required");
  });

  it('returns "Required" for a whitespace-only string', () => {
    expect(validateRequired("   ")).toBe("Required");
  });

  it('returns "Required" for a tab-only string', () => {
    expect(validateRequired("\t")).toBe("Required");
  });
});

describe("validateEmail", () => {
  it("returns undefined for a valid email", () => {
    expect(validateEmail("user@example.com")).toBeUndefined();
  });

  it("returns undefined for an email with subdomain", () => {
    expect(validateEmail("user@mail.example.com")).toBeUndefined();
  });

  it("returns undefined for an email with plus addressing", () => {
    expect(validateEmail("user+tag@example.com")).toBeUndefined();
  });

  it('returns "Required" for an empty string', () => {
    expect(validateEmail("")).toBe("Required");
  });

  it('returns "Required" for null', () => {
    expect(validateEmail(null)).toBe("Required");
  });

  it('returns "Required" for undefined', () => {
    expect(validateEmail(undefined)).toBe("Required");
  });

  it('returns "Required" for a whitespace-only string', () => {
    expect(validateEmail("   ")).toBe("Required");
  });

  it('returns "Invalid email format" for a string without @', () => {
    expect(validateEmail("userexample.com")).toBe("Invalid email format");
  });

  it('returns "Invalid email format" for a string without domain', () => {
    expect(validateEmail("user@")).toBe("Invalid email format");
  });

  it('returns "Invalid email format" for a string without local part', () => {
    expect(validateEmail("@example.com")).toBe("Invalid email format");
  });

  it('returns "Invalid email format" for an email with spaces', () => {
    expect(validateEmail("user @example.com")).toBe("Invalid email format");
  });

  it('returns "Invalid email format" for a string without TLD', () => {
    expect(validateEmail("user@example")).toBe("Invalid email format");
  });
});
