import { Location } from "history";
import { useCallback, useEffect, useState } from "react";
import { history } from "../history";

export function useLocationState<T>(
  name: string,
  initialValue: T
): [T, (v: T) => void] {
  const [stateValue, setStateValue] = useState<T>(() =>
    getLocationValue(history.location, name, initialValue)
  );

  useEffect(() => {
    return history.listen(({ location }) =>
      setStateValue(getLocationValue(location, name, initialValue))
    );
  }, [name, initialValue]);

  const setValue = useCallback(
    (newValue: T) => {
      history.replace(history.location, {
        ...(history.location.state as any),
        [name]: newValue,
      });
    },
    [name]
  );

  return [stateValue, setValue];
}

function getLocationValue<T>(
  location: Location,
  name: string,
  initialValue: T
) {
  const state = location.state as Record<string, any>;
  return state && name in state ? state[name] : initialValue;
}
