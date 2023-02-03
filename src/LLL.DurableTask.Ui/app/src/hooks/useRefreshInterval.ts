import { Dispatch, useCallback } from "react";
import { useLocalStorage } from "react-use";

export function useRefreshInterval(
  name: string
): [number | undefined, Dispatch<number | undefined>] {
  const [value, setValue, remove] = useLocalStorage<number | undefined>(
    `refresh-interval/${name}`
  );

  const customSetValue = useCallback(
    (newValue: number | undefined) => {
      if (newValue) {
        setValue(newValue);
      } else {
        remove();
      }
    },
    [remove, setValue]
  );

  return [value, customSetValue];
}
