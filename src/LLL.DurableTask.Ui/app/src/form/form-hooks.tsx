import React from "react";
import { reach, Schema, ValidationError } from "yup";
import { Observe } from "./observation-components";
import { useObserver, useObserverEffect } from "./observation-hooks";
import { unwrap } from "./observation-proxy";

export class Field<T> {
  private cache: Map<PropertyKey, Field<any>> = new Map();

  constructor(
    public form: Form<any>,
    public parent: Field<any> | Form<any>,
    public path: string,
    public prop: PropertyKey,
    public schema?: any
  ) {}

  field<P extends keyof T>(prop: P): Field<T[P]> {
    let field = this.cache.get(prop);
    if (field) {
      return field;
    }

    const newPath =
      typeof prop === "string"
        ? this.path
          ? `${this.path}.${prop}`
          : prop
        : `${this.path}[${prop}]`;

    field = new Field<T[P]>(
      this.form,
      this,
      newPath,
      prop,
      this.schema && tryReach(this.schema, String(prop))
    );

    this.cache.set(prop, field);

    return field;
  }

  render(fn: (field: Field<T>) => React.ReactNode) {
    return (
      <Observe field={this}>
        {({ field }) => {
          return fn(field);
        }}
      </Observe>
    );
  }

  map<R>(
    fn: (
      field: Field<T extends readonly (infer E)[] ? E : T[keyof T]>,
      property: T extends readonly (infer E)[] ? number : string
    ) => R
  ): R[] {
    const value = this.value;
    if (Array.isArray(value)) {
      const result: R[] = [];
      for (let i = 0; i < value.length; i++) {
        result.push(fn(this.field(i as any) as any, i as any));
      }
      return result;
    } else {
      return Object.keys(this.value).map((key: any) => {
        return fn(this.field(key) as any, key as any);
      });
    }
  }

  get value(): T {
    return this.parent.value?.[this.prop];
  }

  set value(newValue: T) {
    let parentValue = this.parent.value;
    if (!parentValue) {
      if (typeof this.prop === "number") {
        parentValue = [] as any;
        this.parent.value = parentValue;
      } else {
        parentValue = {} as any;
        this.parent.value = parentValue;
      }
    }
    parentValue[this.prop] = newValue;
  }

  push(...values: any[]) {
    (this.value as any).push(...values);
  }

  remove(item: any) {
    var value = this.value as any;
    const index = value.findIndex((v: any) => unwrap(v) === unwrap(item));
    if (index !== -1) {
      value.splice(index, 1);
    }
  }

  get errorMessage() {
    return this.form.errors[this.path];
  }

  get hasError() {
    return Boolean(this.errorMessage);
  }

  get label() {
    return this.schema?._label;
  }

  get required() {
    return (
      this.schema &&
      Boolean(
        this.schema.tests.find(
          (t: any) => t.name === "required" || t.OPTIONS?.name === "required"
        )
      )
    );
  }
}

export class Form<T> {
  private cache: Map<PropertyKey, Field<any>> = new Map();

  public value: T;
  public errors: Record<string, string>;
  public pendingValidation = true;

  constructor(public valueFactory: () => T, public schema: Schema<T>) {
    this.value = valueFactory();
    this.errors = {};
  }

  public reset() {
    this.value = this.valueFactory();
  }

  public field<P extends keyof T>(prop: P): Field<T[P]> {
    let field = this.cache.get(prop);
    if (field) {
      return field;
    }

    field = new Field<T[P]>(
      this,
      this,
      String(prop),
      prop,
      this.schema && tryReach(this.schema, String(prop))
    );

    this.cache.set(prop, field);

    return field;
  }
}

export function useForm<T>(schema: Schema<T>, valueFactory: () => T): Form<T> {
  const { useObservableState } = useObserver("Form");

  const [form] = useObservableState(() => new Form<T>(valueFactory, schema));

  useObserverEffect(
    form,
    async ({ value, errors }) => {
      if (!schema) return;

      try {
        await schema.validate(value, {
          strict: true,
          abortEarly: false,
        });
        for (let path in errors) {
          delete errors[path];
        }
      } catch (e) {
        if (e instanceof ValidationError) {
          const newErrors: Record<string, ValidationError[]> = {};
          for (let error of e.inner) {
            if (!newErrors[error.path]) {
              newErrors[error.path] = [];
            }
            newErrors[error.path].push(error);
          }
          for (let path in errors) {
            if (!(path in newErrors)) {
              delete errors[path];
            }
          }
          for (let path in newErrors) {
            errors[path] = newErrors[path].map((e) => e.message).join(" ");
          }
        }
      } finally {
        form.pendingValidation = false;
      }
    },
    [],
    250,
    () => {
      form.pendingValidation = true;
    }
  );

  return form;
}

function tryReach(schema: any, prop: string) {
  try {
    return reach(schema, prop);
  } catch {
    return undefined;
  }
}
