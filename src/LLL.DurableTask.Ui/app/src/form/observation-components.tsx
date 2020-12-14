import React from "react";
import { useObserver } from "./observation-hooks";

type ObserveProps<T> = {
  children?: (props: Omit<T, "children">) => React.ReactElement | null;
};

export const Observe: <T extends ObserveProps<T>>(
  props: T
) => React.ReactElement<any, any> | null = React.memo(
  function (props) {
    const { children, ...other } = props;
    const { observe } = useObserver((props as any).field?.path);
    if (!children) return null;
    return children(observe(other));
  },
  (previous: any, next: any) => {
    for (let key in next) {
      if (key === "children") continue;
      if (previous[key] !== next[key]) {
        return false;
      }
    }
    for (let key in previous) {
      if (key in next) continue;
      if (previous[key] !== next[key]) {
        return false;
      }
    }
    return true;
  }
);
