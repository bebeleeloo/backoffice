import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import { CreateRoleDialog, EditRoleDialog } from "../../pages/RoleDialogs";
import { ALL_PERMISSIONS, buildRoleDto } from "../factories";

describe("CreateRoleDialog", () => {
  it("renders form fields", () => {
    renderWithProviders(<CreateRoleDialog open onClose={() => {}} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Create Role")).toBeInTheDocument();
    expect(screen.getByLabelText(/^name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/description/i)).toBeInTheDocument();
  });

  it("shows validation error for empty name", async () => {
    const { user } = renderWithProviders(<CreateRoleDialog open onClose={() => {}} />, {
      permissions: ALL_PERMISSIONS,
    });

    await user.click(screen.getByRole("button", { name: /^create$/i }));

    expect(screen.getByText(/required/i)).toBeInTheDocument();
  });

  it("renders Create and Cancel buttons", () => {
    renderWithProviders(<CreateRoleDialog open onClose={() => {}} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /^create$/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /cancel/i })).toBeInTheDocument();
  });
});

describe("EditRoleDialog", () => {
  it("populates form with role data", () => {
    const role = buildRoleDto({
      id: "r1",
      name: "Manager",
      description: "Manage users",
      isSystem: false,
    });

    renderWithProviders(<EditRoleDialog open onClose={() => {}} role={role} />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText(/edit role: manager/i)).toBeInTheDocument();
    expect(screen.getByDisplayValue("Manager")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Manage users")).toBeInTheDocument();
  });

  it("disables name field for system roles", () => {
    const role = buildRoleDto({
      name: "Admin",
      isSystem: true,
    });

    renderWithProviders(<EditRoleDialog open onClose={() => {}} role={role} />, {
      permissions: ALL_PERMISSIONS,
    });

    const nameInput = screen.getByDisplayValue("Admin");
    expect(nameInput).toBeDisabled();
  });
});
