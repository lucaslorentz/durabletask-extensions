import { useRef, useState } from "react";

type DynamicRefsResult = [
  <T>(name: string | number) => T,
  <T>(name: string | number) => (value: T) => void
];

export function useDynamicRefs(
  updateOnChange: boolean = false
): DynamicRefsResult {
  const ref = useRef<DynamicRefsResult>();
  const [, setState] = useState(0);
  if (ref.current == null) {
    const values: Record<string | number, any> = {};
    const callbacks: Record<string | number, (v: any) => void> = {};
    ref.current = [
      (name) => values[name],
      (name) => {
        if (name in callbacks) return callbacks[name];
        return (callbacks[name] = (value) => {
          values[name] = value;
          if (updateOnChange) {
            setState((v) => v + 1);
          }
        });
      },
    ];
  }
  return ref.current!;
}
