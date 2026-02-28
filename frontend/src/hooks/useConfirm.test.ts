import { describe, it, expect } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useConfirm } from "./useConfirm";

describe("useConfirm", () => {
  it("has open=false in initial state", () => {
    const { result } = renderHook(() => useConfirm());

    expect(result.current.confirmDialogProps.open).toBe(false);
  });

  it("initial state has empty title and message", () => {
    const { result } = renderHook(() => useConfirm());

    expect(result.current.confirmDialogProps.title).toBe("");
    expect(result.current.confirmDialogProps.message).toBe("");
  });

  it("confirm() opens the dialog and sets options", () => {
    const { result } = renderHook(() => useConfirm());

    act(() => {
      result.current.confirm({
        title: "Delete item?",
        message: "This action cannot be undone.",
        confirmLabel: "Delete",
      });
    });

    expect(result.current.confirmDialogProps.open).toBe(true);
    expect(result.current.confirmDialogProps.title).toBe("Delete item?");
    expect(result.current.confirmDialogProps.message).toBe(
      "This action cannot be undone.",
    );
    expect(result.current.confirmDialogProps.confirmLabel).toBe("Delete");
  });

  it("confirm() returns a promise that resolves to true on onConfirm", async () => {
    const { result } = renderHook(() => useConfirm());

    let promise: Promise<boolean>;
    act(() => {
      promise = result.current.confirm({
        title: "Confirm?",
        message: "Are you sure?",
      });
    });

    expect(result.current.confirmDialogProps.open).toBe(true);

    act(() => {
      result.current.confirmDialogProps.onConfirm();
    });

    const resolved = await promise!;
    expect(resolved).toBe(true);
    expect(result.current.confirmDialogProps.open).toBe(false);
  });

  it("confirm() returns a promise that resolves to false on onCancel", async () => {
    const { result } = renderHook(() => useConfirm());

    let promise: Promise<boolean>;
    act(() => {
      promise = result.current.confirm({
        title: "Confirm?",
        message: "Are you sure?",
      });
    });

    expect(result.current.confirmDialogProps.open).toBe(true);

    act(() => {
      result.current.confirmDialogProps.onCancel();
    });

    const resolved = await promise!;
    expect(resolved).toBe(false);
    expect(result.current.confirmDialogProps.open).toBe(false);
  });

  it("passes isLoading option through to dialog props", () => {
    const { result } = renderHook(() => useConfirm());

    act(() => {
      result.current.confirm({
        title: "Loading test",
        message: "Please wait...",
        isLoading: true,
      });
    });

    expect(result.current.confirmDialogProps.isLoading).toBe(true);
  });
});
