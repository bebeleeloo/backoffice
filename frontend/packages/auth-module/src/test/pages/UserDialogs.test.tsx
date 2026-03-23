import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import { CreateUserDialog, EditUserDialog, ResetPasswordDialog } from "../../pages/UserDialogs";
import { ALL_PERMISSIONS, buildUserDto } from "../factories";

describe("CreateUserDialog", () => {
  it("renders all form fields", () => {
    renderWithProviders(<CreateUserDialog open onClose={() => {}} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Create User")).toBeInTheDocument();
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/full name/i)).toBeInTheDocument();
  });

  it("shows validation errors for empty required fields", async () => {
    const { user } = renderWithProviders(<CreateUserDialog open onClose={() => {}} />, {
      permissions: ALL_PERMISSIONS,
    });

    await user.click(screen.getByRole("button", { name: /^create$/i }));

    const errors = screen.getAllByText(/required/i);
    expect(errors.length).toBeGreaterThanOrEqual(2);
  });

  it("renders Create and Cancel buttons", () => {
    renderWithProviders(<CreateUserDialog open onClose={() => {}} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /^create$/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /cancel/i })).toBeInTheDocument();
  });
});

describe("EditUserDialog", () => {
  it("populates form with user data", () => {
    const user = buildUserDto({
      id: "u1",
      username: "alice",
      email: "alice@test.com",
      fullName: "Alice Smith",
      isActive: true,
    });

    renderWithProviders(<EditUserDialog open onClose={() => {}} user={user} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText(/edit user: alice/i)).toBeInTheDocument();
    expect(screen.getByDisplayValue("alice@test.com")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Alice Smith")).toBeInTheDocument();
  });

  it("renders Save and Cancel buttons", () => {
    const user = buildUserDto();

    renderWithProviders(<EditUserDialog open onClose={() => {}} user={user} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /save/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /cancel/i })).toBeInTheDocument();
  });
});

describe("ResetPasswordDialog", () => {
  it("renders password fields", () => {
    const user = buildUserDto({ username: "alice" });

    renderWithProviders(<ResetPasswordDialog open onClose={() => {}} user={user} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText(/reset password: alice/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/new password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument();
  });

  it("shows validation error for short password", async () => {
    const userData = buildUserDto({ username: "alice" });
    const { user } = renderWithProviders(<ResetPasswordDialog open onClose={() => {}} user={userData} />, {
      permissions: ALL_PERMISSIONS,
    });

    await user.type(screen.getByLabelText(/new password/i), "abc");
    await user.type(screen.getByLabelText(/confirm password/i), "abc");
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    expect(screen.getByText(/at least 6 characters/i)).toBeInTheDocument();
  });
});
