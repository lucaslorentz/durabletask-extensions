import {
  useEffect,
  useLayoutEffect,
  useMemo,
  useReducer,
  useRef,
  useState,
} from "react";
import { createProxyObserver } from "./observation-proxy";

export function useObserver(description: string) {
  const [, forceUpdate] = useReducer((x) => x + 1, 0);

  const proxyObserver = useMemo(() => createProxyObserver(description), [description]);

  const { deactivate, dispose } = proxyObserver.activate(forceUpdate);

  useLayoutEffect(() => {
    deactivate();
    return dispose;
  });

  return {
    observe: proxyObserver.observe,
    useObservableState<S>(initialState: S | (() => S)): [S, (v: S) => void] {
      const [value, setValue] = useState<S>(initialState);
      return [proxyObserver.observe(value), setValue];
    },
  };
}

export function useObserverEffect<T>(
  observableDeps: T,
  fn: (data: T) => PromiseLike<any> | void,
  nonObservableDeps?: any[],
  debounceMs: number = 0,
  onSchedule?: () => void
) {
  const proxyContext = createProxyObserver('Effect');

  let lastDispose = useRef<(() => void) | undefined>();
  let lastSetTimeoutId = useRef<any>();

  useEffect(schedule, nonObservableDeps);
  useEffect(() => disposeLast, []);

  function schedule() {
    lastSetTimeoutId.current && clearTimeout(lastSetTimeoutId.current);
    lastSetTimeoutId.current = setTimeout(invoke, debounceMs);
    onSchedule?.();
  }

  function disposeLast() {
    if (lastDispose.current) {
      lastDispose.current();
      lastDispose.current = undefined;
    }
  }

  async function invoke() {
    lastSetTimeoutId.current = undefined;

    disposeLast();

    const { deactivate, dispose } = proxyContext.activate(schedule);

    lastDispose.current = dispose;

    const maybePromise = fn(proxyContext.observe(observableDeps));

    await maybePromise;

    deactivate();
  }
}
