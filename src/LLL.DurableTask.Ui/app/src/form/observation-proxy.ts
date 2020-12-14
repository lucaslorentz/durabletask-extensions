import {
  notifyChange,
  ObservationDisposer,
  observeChange,
} from "./observation";

export type ProxyObserver = {
  observe(value: any): any;
  activate(
    onChange: () => void
  ): { deactivate: () => void; dispose: () => void };
};

export function createProxyObserver(description: string) {
  const proxies: WeakMap<any, any> = new WeakMap();

  let onReadValue: ((target: any, p: PropertyKey) => void) | undefined;

  const context: ProxyObserver = {
    observe(value: any) {
      if (typeof value !== "object") {
        return value;
      }
      value = unwrap(value);
      let proxy = proxies.get(value);
      if (!proxy) {
        proxy = new Proxy(value, handler);
        proxies.set(value, proxy);
      }
      return proxy;
    },
    activate(onChange: () => void) {
      const observationsDisposers: Map<
        any,
        Map<PropertyKey, ObservationDisposer>
      > = new Map();

      onReadValue = (target: any, p: PropertyKey) => {
        let targetDisposers = observationsDisposers.get(target);
        if (!targetDisposers) {
          targetDisposers = new Map();
          observationsDisposers.set(target, targetDisposers);
        }
        if (targetDisposers.has(p)) return;
        targetDisposers.set(p, observeChange(target, p, onChange));
      };

      return {
        deactivate: () => {
          onReadValue = undefined;
        },
        dispose: () => {
          for (let targetDisposers of observationsDisposers.values()) {
            for (let dispose of targetDisposers.values()) {
              dispose();
            }
          }

          observationsDisposers.clear();
        },
      };
    },
  };

  const handler: ProxyHandler<any> = {
    get(target: any, p: PropertyKey, receiver: any): any {
      if (p === "__isObservableProxy") {
        return true;
      }
      if (p === "__target") {
        return target;
      }
      if (typeof p === "symbol") {
        return target[p];
      }
      const value = Reflect.get(target, p, receiver);
      onReadValue?.(target, p);
      if (typeof value === "object") {
        return context.observe(value);
      }
      if (typeof value === "function") {
        if (Array.isArray(target)) {
          return wrapArrayMethod(target, p, value);
        } else if (
          target instanceof Map ||
          target instanceof WeakMap ||
          target instanceof Date ||
          target instanceof RegExp
        ) {
          return value.bind(target);
        }
      }
      return value;
    },
    set(target: any, p: PropertyKey, value: any, receiver: any): boolean {
      if (Reflect.get(target, p) === value) {
        return true;
      }
      const hadKey = Reflect.has(target, p);
      Reflect.set(target, p, value, receiver);
      notifyChange(target, p);
      if (!hadKey) {
        notifyChange(target, "$ownKeys");
      }
      return true;
    },
    deleteProperty(target: any, p: PropertyKey): boolean {
      const hadKey = Reflect.has(target, p);
      Reflect.deleteProperty(target, p);
      notifyChange(target, p);
      if (hadKey) {
        notifyChange(target, "$ownKeys");
      }
      return true;
    },
    ownKeys(target: any): PropertyKey[] {
      onReadValue?.(target, "$ownKeys");
      return Reflect.ownKeys(target);
    },
  };

  return context;
}

function wrapArrayMethod(target: any, p: PropertyKey, method: Function) {
  switch (p) {
    case "push":
      return function (...items: any[]) {
        const newLength = target.push(...items);
        notifyChange(target, "length");
        notifyChange(target, "$ownKeys");
        for (let i = newLength - items.length; i < newLength; i++) {
          notifyChange(target, i);
        }
        return newLength;
      };
    case "pop":
      return function () {
        const oldLength = target.length;
        const result = target.pop();
        notifyChange(target, "length");
        notifyChange(target, "$ownKeys");
        if (oldLength !== target.length) {
          notifyChange(target, target.length - 1);
        }
        return result;
      };
    case "unshift":
      return function (...items: any[]) {
        const newLength = target.unshift(...items);
        notifyChange(target, "length");
        notifyChange(target, "$ownKeys");
        for (let i = 0; i < newLength; i++) {
          notifyChange(target, i);
        }
        return newLength;
      };
    case "shift":
      return function () {
        const item = target.shift();
        notifyChange(target, "length");
        notifyChange(target, "$ownKeys");
        for (let i = 0, l = target.length; i < l; i++) {
          notifyChange(target, i);
        }
        return item;
      };
    case "splice":
      return function (start: number, deleteCount: number, ...items: any[]) {
        const removedItems = target.splice(start, deleteCount, ...items);
        notifyChange(target, "length");
        notifyChange(target, "$ownKeys");
        for (let i = start, l = target.length; i < l; i++) {
          notifyChange(target, i);
        }
        return removedItems;
      };
    default:
      return method;
  }
}

export function unwrap(value: any) {
  if (value.__isObservableProxy) {
    return value.__target;
  }
  return value;
}
