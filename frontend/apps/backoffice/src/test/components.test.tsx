import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "./renderWithProviders";
import { ConfirmDialog, ErrorBoundary, PageContainer, UserAvatar } from "@broker/ui-kit";

describe("ConfirmDialog", () => {
  it("renders title and message when open", () => {
    renderWithProviders(
      <ConfirmDialog
        open={true}
        title="Confirm Delete"
        message="Are you sure you want to delete this item?"
        onConfirm={() => {}}
        onCancel={() => {}}
      />,
    );

    expect(screen.getByText("Confirm Delete")).toBeInTheDocument();
    expect(screen.getByText("Are you sure you want to delete this item?")).toBeInTheDocument();
  });

  it("calls onConfirm when Delete button clicked", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();

    renderWithProviders(
      <ConfirmDialog
        open={true}
        title="Delete"
        message="Sure?"
        onConfirm={onConfirm}
        onCancel={() => {}}
      />,
    );

    await user.click(screen.getByRole("button", { name: /delete/i }));
    expect(onConfirm).toHaveBeenCalledOnce();
  });

  it("calls onCancel when Cancel button clicked", async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();

    renderWithProviders(
      <ConfirmDialog
        open={true}
        title="Delete"
        message="Sure?"
        onConfirm={() => {}}
        onCancel={onCancel}
      />,
    );

    await user.click(screen.getByRole("button", { name: /cancel/i }));
    expect(onCancel).toHaveBeenCalledOnce();
  });
});

describe("ErrorBoundary", () => {
  it("renders children normally", () => {
    renderWithProviders(
      <ErrorBoundary>
        <div>Normal content</div>
      </ErrorBoundary>,
    );

    expect(screen.getByText("Normal content")).toBeInTheDocument();
  });

  it("shows error message when child throws", () => {
    const ThrowingComponent = () => {
      throw new Error("Test error");
    };

    // Suppress console.error from ErrorBoundary
    const spy = vi.spyOn(console, "error").mockImplementation(() => {});

    renderWithProviders(
      <ErrorBoundary>
        <ThrowingComponent />
      </ErrorBoundary>,
    );

    expect(screen.getByText("Something went wrong")).toBeInTheDocument();

    spy.mockRestore();
  });
});

describe("UserAvatar", () => {
  it("renders img src with photo URL when hasPhoto is true", () => {
    renderWithProviders(
      <UserAvatar userId="user-123" name="Alice Smith" hasPhoto={true} />,
    );

    const avatar = screen.getByRole("img");
    expect(avatar).toHaveAttribute("src", "/api/v1/users/user-123/photo");
  });

  it("renders initial letter when hasPhoto is false", () => {
    renderWithProviders(
      <UserAvatar userId="user-456" name="Bob Jones" hasPhoto={false} />,
    );

    expect(screen.getByText("B")).toBeInTheDocument();
  });
});

describe("PageContainer", () => {
  it("renders title text", () => {
    renderWithProviders(
      <PageContainer title="Test Page Title">
        <div>Content</div>
      </PageContainer>,
    );

    expect(screen.getByText("Test Page Title")).toBeInTheDocument();
  });
});
