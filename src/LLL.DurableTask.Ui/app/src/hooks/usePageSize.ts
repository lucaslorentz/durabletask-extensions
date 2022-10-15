import { Dispatch } from "react";
import { useLocalStorage } from "react-use";

export function usePageSize(): [number, Dispatch<number>] {
  const [value, setValue] = useLocalStorage<number>("pageSize");
  return [value ?? 10, setValue];
}
