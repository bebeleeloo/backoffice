import { validateRequired, validateEmail } from "./validateFields";

describe("validateRequired", () => {
  it("returns 'Required' for empty string", () => {
    expect(validateRequired("")).toBe("Required");
  });

  it("returns 'Required' for whitespace-only string", () => {
    expect(validateRequired("   ")).toBe("Required");
  });

  it("returns 'Required' for null", () => {
    expect(validateRequired(null)).toBe("Required");
  });

  it("returns 'Required' for undefined", () => {
    expect(validateRequired(undefined)).toBe("Required");
  });

  it("returns undefined for valid string", () => {
    expect(validateRequired("hello")).toBeUndefined();
  });

  it("returns undefined for string with leading/trailing spaces", () => {
    expect(validateRequired("  hello  ")).toBeUndefined();
  });
});

describe("validateEmail", () => {
  it("returns 'Required' for empty string", () => {
    expect(validateEmail("")).toBe("Required");
  });

  it("returns 'Required' for null", () => {
    expect(validateEmail(null)).toBe("Required");
  });

  it("returns 'Required' for undefined", () => {
    expect(validateEmail(undefined)).toBe("Required");
  });

  it("returns 'Required' for whitespace-only string", () => {
    expect(validateEmail("   ")).toBe("Required");
  });

  it("returns 'Invalid email format' for missing @", () => {
    expect(validateEmail("notanemail")).toBe("Invalid email format");
  });

  it("returns 'Invalid email format' for missing domain", () => {
    expect(validateEmail("user@")).toBe("Invalid email format");
  });

  it("returns 'Invalid email format' for missing local part", () => {
    expect(validateEmail("@domain.com")).toBe("Invalid email format");
  });

  it("returns 'Invalid email format' for spaces in email", () => {
    expect(validateEmail("user @domain.com")).toBe("Invalid email format");
  });

  it("returns undefined for valid email", () => {
    expect(validateEmail("user@example.com")).toBeUndefined();
  });

  it("returns undefined for email with subdomain", () => {
    expect(validateEmail("user@mail.example.com")).toBeUndefined();
  });

  it("returns undefined for email with plus sign", () => {
    expect(validateEmail("user+tag@example.com")).toBeUndefined();
  });
});
