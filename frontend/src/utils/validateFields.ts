export type FieldErrors = Record<string, string | undefined>;

export function validateRequired(value: string | undefined | null): string | undefined {
  if (!value || !value.trim()) return "Required";
}

export function validateEmail(value: string | undefined | null): string | undefined {
  if (!value || !value.trim()) return "Required";
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return "Invalid email format";
}
