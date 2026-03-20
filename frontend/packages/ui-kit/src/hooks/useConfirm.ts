import { useState, useCallback, useRef } from "react";

interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  isLoading?: boolean;
}

export function useConfirm() {
  const [open, setOpen] = useState(false);
  const [options, setOptions] = useState<ConfirmOptions>({ title: "", message: "" });
  const resolveRef = useRef<((value: boolean) => void) | null>(null);

  const confirm = useCallback((opts: ConfirmOptions) => {
    setOptions(opts);
    setOpen(true);
    return new Promise<boolean>((resolve) => {
      resolveRef.current = resolve;
    });
  }, []);

  const onConfirm = useCallback(() => {
    setOpen(false);
    resolveRef.current?.(true);
    resolveRef.current = null;
  }, []);

  const onCancel = useCallback(() => {
    setOpen(false);
    resolveRef.current?.(false);
    resolveRef.current = null;
  }, []);

  return {
    confirm,
    confirmDialogProps: {
      open,
      title: options.title,
      message: options.message,
      confirmLabel: options.confirmLabel,
      onConfirm,
      onCancel,
      isLoading: options.isLoading,
    },
  };
}
