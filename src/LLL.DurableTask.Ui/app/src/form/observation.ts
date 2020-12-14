export type Observer = (target: any, p: PropertyKey) => void;
export type ObservationDisposer = () => void;

const observers: Map<any, Map<PropertyKey, Map<Observer, number>>> = new Map();

export function notifyChange(target: any, property: PropertyKey) {
  let targetObservers = observers.get(target);
  if (!targetObservers) {
    return;
  }

  property = String(property);
  let propertyObservers = targetObservers.get(property);
  if (!propertyObservers) {
    return;
  }

  Array.from(propertyObservers.keys()).forEach((observer) => {
    observer(target, property)
  });
}

export function observeChange(
  target: any,
  property: PropertyKey,
  fn: Observer
): ObservationDisposer {
  let targetObservers = observers.get(target);
  if (!targetObservers) {
    targetObservers = new Map();
    observers.set(target, targetObservers);
  }

  property = String(property);
  let propertyObservers = targetObservers.get(property);
  if (!propertyObservers) {
    propertyObservers = new Map();
    targetObservers.set(property, propertyObservers);
  }

  const count = propertyObservers.get(fn) ?? 0;
  propertyObservers.set(fn, count + 1);

  let disposed = false;
  return () => {
    if (disposed) return;
    disposed = true;

    let count = propertyObservers!.get(fn)!;
    propertyObservers!.set(fn, --count);

    if (count === 0) {
      propertyObservers!.delete(fn);

      if (propertyObservers!.size === 0) {
        targetObservers!.delete(property);

        if (targetObservers!.size === 0) {
          observers.delete(target);
        }
      }
    }
  };
}
