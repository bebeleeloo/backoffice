import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import { ProfileTab } from "../../pages/ProfileTab";
import { ALL_PERMISSIONS, buildUserProfile } from "../factories";

describe("ProfileTab", () => {
  it("renders profile info section", () => {
    renderWithProviders(<ProfileTab />, {
      permissions: ALL_PERMISSIONS,
      user: buildUserProfile({ username: "admin", fullName: "Test Admin" }),
    });

    expect(screen.getByText("Profile Info")).toBeInTheDocument();
    expect(screen.getByText("admin")).toBeInTheDocument();
  });

  it("renders edit profile fields", () => {
    renderWithProviders(<ProfileTab />, {
      permissions: ALL_PERMISSIONS,
      user: buildUserProfile({ fullName: "Test Admin", email: "admin@test.com" }),
    });

    expect(screen.getByText("Edit Profile")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Test Admin")).toBeInTheDocument();
    expect(screen.getByDisplayValue("admin@test.com")).toBeInTheDocument();
  });

  it("renders change password section with fields", () => {
    renderWithProviders(<ProfileTab />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByLabelText("Current Password")).toBeInTheDocument();
    expect(screen.getByLabelText("New Password")).toBeInTheDocument();
    expect(screen.getByLabelText("Confirm New Password")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /change password/i })).toBeInTheDocument();
  });

  it("renders photo section with Change Photo button", () => {
    renderWithProviders(<ProfileTab />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /change photo/i })).toBeInTheDocument();
  });

  it("renders Save button in edit profile section", () => {
    renderWithProviders(<ProfileTab />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /^save$/i })).toBeInTheDocument();
  });
});
