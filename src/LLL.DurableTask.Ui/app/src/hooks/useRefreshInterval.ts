import { Dispatch, useCallback } from "react";
import { useLocalStorage } from "react-use";

export function useRefreshInterval(): [
  number | undefined,
  Dispatch<number | undefined>
] {
  const [value, setValue, remove] = useLocalStorage<number | undefined>(
    "refreshInterval"
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
