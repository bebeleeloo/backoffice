import { screen, render, act } from "@testing-library/react";
import { useState } from "react";
import { useDebounce } from "./useDebounce";

function HookHarness({ value, delay }: { value: string; delay?: number }) {
  const debounced = useDebounce(value, delay);
  return <span data-testid="output">{debounced}</span>;
}

function Interactive({ delay }: { delay?: number }) {
  const [raw, setRaw] = useState("a");
  const debounced = useDebounce(raw, delay);
  return (
    <>
      <button onClick={() => setRaw((p) => p + "b")}>append</button>
      <span data-testid="output">{debounced}</span>
    </>
  );
}

describe("useDebounce", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns the initial value immediately", () => {
    render(<HookHarness value="hello" />);
    expect(screen.getByTestId("output")).toHaveTextContent("hello");
  });

  it("delays update by default 300ms", () => {
    const { rerender } = render(<HookHarness value="a" />);

    rerender(<HookHarness value="ab" />);
    expect(screen.getByTestId("output")).toHaveTextContent("a");

    act(() => {
      vi.advanceTimersByTime(299);
    });
    expect(screen.getByTestId("output")).toHaveTextContent("a");

    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(screen.getByTestId("output")).toHaveTextContent("ab");
  });

  it("resets timer on rapid changes", () => {
    const { rerender } = render(<HookHarness value="a" />);

    rerender(<HookHarness value="ab" />);
    act(() => {
      vi.advanceTimersByTime(200);
    });

    rerender(<HookHarness value="abc" />);
    act(() => {
      vi.advanceTimersByTime(200);
    });
    expect(screen.getByTestId("output")).toHaveTextContent("a");

    act(() => {
      vi.advanceTimersByTime(100);
    });
    expect(screen.getByTestId("output")).toHaveTextContent("abc");
  });

  it("supports a custom delay", () => {
    const { rerender } = render(<HookHarness value="x" delay={500} />);

    rerender(<HookHarness value="xy" delay={500} />);

    act(() => {
      vi.advanceTimersByTime(499);
    });
    expect(screen.getByTestId("output")).toHaveTextContent("x");

    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(screen.getByTestId("output")).toHaveTextContent("xy");
  });

  it("works with state-driven updates", () => {
    render(<Interactive />);
    expect(screen.getByTestId("output")).toHaveTextContent("a");

    act(() => {
      screen.getByText("append").click();
    });
    expect(screen.getByTestId("output")).toHaveTextContent("a");

    act(() => {
      vi.advanceTimersByTime(300);
    });
    expect(screen.getByTestId("output")).toHaveTextContent("ab");
  });
});
