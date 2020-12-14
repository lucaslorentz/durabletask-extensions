import { useCallback, useEffect, useRef } from "react";

export function useDebouncedEffect(
  fn: () => void,
  dependencies: any[],
  ms: number,
  debounceFirstExecution = false,
  onSchedule?: () => void
): [() => void] {
  const shouldDebounce = useRef(debounceFirstExecution);
  const timeoutId = useRef<any>(null);

  useEffect(() => {
    onSchedule?.();

    if (timeoutId.current) {
      clearTimeout(timeoutId.current);
      timeoutId.current = null;
    }
    if (shouldDebounce.current) {
      timeoutId.current = setTimeout(fn, ms);
    } else {
      shouldDebounce.current = true;
      fn();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ms, ...dependencies]);

  const skipDebounce = useCallback(() => {
    shouldDebounce.current = false;
  }, []);

  return [skipDebounce];
}
