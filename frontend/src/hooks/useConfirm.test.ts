import { renderHook, act } from "@testing-library/react";
import { useConfirm } from "./useConfirm";

describe("useConfirm", () => {
  it("starts with dialog closed", () => {
    const { result } = renderHook(() => useConfirm());
    expect(result.current.confirmDialogProps.open).toBe(false);
  });

  it("confirm() opens dialog and sets options", () => {
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
    expect(result.current.confirmDialogProps.message).toBe("This action cannot be undone.");
    expect(result.current.confirmDialogProps.confirmLabel).toBe("Delete");
  });

  it("onConfirm() resolves with true and closes dialog", async () => {
    const { result } = renderHook(() => useConfirm());

    let promise!: Promise<boolean>;
    act(() => {
      promise = result.current.confirm({ title: "Confirm?", message: "Sure?" });
    });

    expect(result.current.confirmDialogProps.open).toBe(true);

    await act(async () => {
      result.current.confirmDialogProps.onConfirm();
    });

    expect(result.current.confirmDialogProps.open).toBe(false);
    await expect(promise).resolves.toBe(true);
  });

  it("onCancel() resolves with false and closes dialog", async () => {
    const { result } = renderHook(() => useConfirm());

    let promise!: Promise<boolean>;
    act(() => {
      promise = result.current.confirm({ title: "Confirm?", message: "Sure?" });
    });

    expect(result.current.confirmDialogProps.open).toBe(true);

    await act(async () => {
      result.current.confirmDialogProps.onCancel();
    });

    expect(result.current.confirmDialogProps.open).toBe(false);
    await expect(promise).resolves.toBe(false);
  });

  it("supports sequential confirmations", async () => {
    const { result } = renderHook(() => useConfirm());

    let promise1!: Promise<boolean>;
    act(() => {
      promise1 = result.current.confirm({ title: "First?", message: "First" });
    });
    await act(async () => {
      result.current.confirmDialogProps.onConfirm();
    });
    await expect(promise1).resolves.toBe(true);

    let promise2!: Promise<boolean>;
    act(() => {
      promise2 = result.current.confirm({ title: "Second?", message: "Second" });
    });
    await act(async () => {
      result.current.confirmDialogProps.onCancel();
    });
    await expect(promise2).resolves.toBe(false);
  });
});
