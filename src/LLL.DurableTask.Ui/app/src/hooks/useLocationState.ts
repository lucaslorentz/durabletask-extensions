import { useCallback, useMemo } from "react";
import { useLocation, useNavigate } from "react-router-dom";

export function useLocationState<T>(
  name: string,
  initialValue: T
): [T, (v: T) => void] {
  const location = useLocation();
  const navigate = useNavigate();

  const stateValue = useMemo(() => {
    const state = location.state as Record<string, any>;
    return state && name in state ? state[name] : initialValue;
  }, [initialValue, location.state, name]);

  const setValue = useCallback(
    (newValue: T) => {
      navigate(
        {
          ...location,
        },
        {
          replace: true,
          state: {
            ...(location.state as any),
            [name]: newValue,
          },
        }
      );
    },
    [location, name, navigate]
  );

  return [stateValue, setValue];
}
