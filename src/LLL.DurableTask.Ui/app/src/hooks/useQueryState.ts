import { Location } from "history";
import { useCallback, useEffect, useState, useRef } from "react";
import { useHistory } from "react-router-dom";

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
  const history = useHistory();
  const { multiple, parse, stringify } = options ?? {};

  const [stateValue, setStateValue] = useState<T>(() =>
    getLocationValue(history.location, name, initialValue, multiple, parse)
  );

  const lastQueryJson = useRef<string>();

  useEffect(() => {
    return history.listen((location) => {
      const locationValue = getLocationValue(
        location,
        name,
        initialValue,
        multiple,
        parse
      );
      const locationValueJson = JSON.stringify(locationValue);
      if (lastQueryJson.current === locationValueJson) return;
      lastQueryJson.current = locationValueJson;
      setStateValue(locationValue);
    });
  }, [history, name, initialValue, multiple, parse]);

  const setValue = useCallback(
    (newValue: any) => {
      const searchParams = new URLSearchParams(history.location.search);

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

      history.replace({
        ...history.location,
        search: "?" + searchParams.toString(),
      });
    },
    [history, name, initialValue, multiple, stringify]
  );

  return [stateValue, setValue];
}

function getLocationValue<T>(
  location: Location,
  name: string,
  initialValue: T | T[],
  multiple?: boolean,
  parse?: any
): any {
  const searchParams = new URLSearchParams(location.search);
  if (!searchParams.has(name)) return initialValue;

  const values = searchParams.getAll(name);

  if (values.length === 0) {
    return initialValue;
  } else if (multiple) {
    if (parse) return values.map((v) => parse(v));
    else return values;
  } else {
    if (parse) return parse(values[0]);
    else return values[0];
  }
}
