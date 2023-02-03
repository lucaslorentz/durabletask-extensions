import { useCallback, useMemo } from "react";
import { useLocation, useNavigate } from "react-router-dom";

type StringOptions = {
  multiple?: false;
  parse?: (v: string) => string;
  stringify?: (v: string) => string;
};

type StringArrayOptions = {
  multiple: true;
  parse?: (v: string) => string;
  stringify?: (v: string) => string;
};

type NonStringOptions<T> = {
  multiple?: false;
  parse: (v: string) => T;
  stringify?: (v: T) => string;
};

type NonStringArrayOptions<T> = {
  multiple: true;
  parse: (v: string) => T extends ReadonlyArray<infer E> ? E : never;
  stringify?: (v: T extends ReadonlyArray<infer E> ? E : never) => string;
};

type Options<T> =
  | StringOptions
  | StringArrayOptions
  | NonStringOptions<T>
  | NonStringArrayOptions<T>;

export function useQueryState<T extends string>(
  name: string,
  initialValue: T,
  options?: StringOptions
): [T, (v: T) => void];
export function useQueryState<T extends ReadonlyArray<string>>(
  name: string,
  initialValue: T,
  options: StringArrayOptions
): [T, (v: T) => void];
export function useQueryState<T extends ReadonlyArray<any>>(
  name: string,
  initialValue: T,
  options: NonStringArrayOptions<T>
): [T, (v: T) => void];
export function useQueryState<T>(
  name: string,
  initialValue: T,
  options: NonStringOptions<T>
): [T, (v: T) => void];
export function useQueryState<T>(
  name: string,
  initialValue: T,
  options?: Options<T>
): [T, (v: T) => void] {
  const { multiple, parse, stringify } = options ?? {};

  const location = useLocation();
  const navigate = useNavigate();

  const searchParams = new URLSearchParams(location.search);
  const valuesJson = JSON.stringify(searchParams.getAll(name));

  const stateValue = useMemo(() => {
    const values = JSON.parse(valuesJson);
    if (values.length === 0) {
      return initialValue;
    } else if (multiple) {
      if (parse) return values.map((v: any) => parse(v));
      else return values;
    } else {
      if (parse) return parse(values[0]);
      else return values[0];
    }
  }, [initialValue, multiple, parse, valuesJson]);

  const setValue = useCallback(
    (newValue: any) => {
      const searchParams = new URLSearchParams(location.search);

      searchParams.delete(name);

      if (newValue !== initialValue) {
        if (multiple) {
          newValue.forEach((v: any) => {
            if (stringify) {
              searchParams.append(name, stringify(v));
            } else {
              searchParams.append(name, v);
            }
          });
        } else {
          if (stringify) {
            searchParams.append(name, stringify(newValue));
          } else {
            searchParams.append(name, newValue);
          }
        }
      }

      navigate(
        {
          ...location,
          search: "?" + searchParams.toString(),
        },
        {
          replace: true,
        }
      );
    },
    [location, name, initialValue, navigate, multiple, stringify]
  );

  return [stateValue, setValue];
}
