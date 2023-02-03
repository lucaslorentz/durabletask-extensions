import { action, autorun, makeObservable, observable, runInAction } from "mobx";
import { Observer } from "mobx-react-lite";
import React, { useState } from "react";
import { reach, SchemaOf, ValidationError } from "yup";

export function useForm<T>(schema: SchemaOf<T>): Form<T> {
  const [form] = useState(() => new Form<T>(schema));

  return form;
}

export class Form<T> {
  private cache: Map<PropertyKey, Field<any>> = new Map();

  public schema: SchemaOf<T>;
  public value: T;
  public errors: Record<string, string>;
  public pendingValidation = true;

  constructor(schema: SchemaOf<T>) {
    this.schema = schema;
    this.value = this.createDefaultValue();
    this.errors = {};

    makeObservable(this, {
      value: observable.deep,
      errors: observable.deep,
      pendingValidation: observable,
      reset: action.bound,
    });

    autorun(
      () => {
        this.validate();
      },
      { delay: 200 }
    );
  }

  public validate() {
    if (!this.schema) return;

    let { value, errors } = this;

    try {
      this.schema.validateSync(value, {
        strict: true,
        abortEarly: false,
      });
      runInAction(() => {
        for (let path in errors) {
          delete errors[path];
        }
      });
    } catch (e) {
      runInAction(() => {
        if (e instanceof ValidationError) {
          const newErrors: Record<string, ValidationError[]> = {};
          for (let error of e.inner) {
            if (!newErrors[error.path!]) {
              newErrors[error.path!] = [];
            }
            newErrors[error.path!].push(error);
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
      });
    } finally {
      runInAction(() => {
        this.pendingValidation = false;
      });
    }
  }

  public render(fn: (form: Form<T>) => React.ReactNode) {
    return <Observer>{() => <>{fn(this)}</>}</Observer>;
  }

  public reset() {
    this.value = this.createDefaultValue();
  }

  public field<P extends keyof T>(prop: P): Field<T[P]> {
    let field = this.cache.get(prop);
    if (field) {
      return field;
    }

    field = new Field<T[P]>(
      this as Form<any>,
      this as Form<any>,
      String(prop),
      prop,
      this.schema && tryReach(this.schema, String(prop))
    );

    this.cache.set(prop, field);

    return field;
  }

  private createDefaultValue() {
    return (this.schema as any).getDefault();
  }
}

export class Field<T> {
  private cache: Map<PropertyKey, Field<any>> = new Map();

  constructor(
    public form: Form<any>,
    public parent: Field<any> | Form<any>,
    public path: string,
    public property: PropertyKey,
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
        : `${this.path}[${String(prop)}]`;

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

  fields(): Field<T extends readonly (infer E)[] ? E : T[keyof T]>[] {
    const value = this.value;
    if (Array.isArray(value)) {
      const fields: any[] = [];
      for (let i = 0; i < value.length; i++) {
        fields.push(this.field(i as any) as any);
      }
      return fields;
    } else {
      return Object.keys(this.value as Object).map((key: any) => {
        return this.field(key);
      }) as any[];
    }
  }

  render(fn: (field: Field<T>) => React.ReactNode) {
    return <Observer>{() => <>{fn(this)}</>}</Observer>;
  }

  get value(): T {
    return this.parent.value?.[this.property];
  }

  set value(newValue: T) {
    runInAction(() => {
      let parentValue = this.parent.value;
      if (!parentValue) {
        if (typeof this.property === "number") {
          parentValue = [] as any;
          this.parent.value = parentValue;
        } else {
          parentValue = {} as any;
          this.parent.value = parentValue;
        }
      }
      parentValue[this.property] = newValue;
    });
  }

  push(...values: any[]) {
    runInAction(() => {
      (this.value as any).push(...values);
    });
  }

  remove(item: any) {
    runInAction(() => {
      var value = this.value as any;
      const index = value.findIndex((v: any) => v === item);
      if (index !== -1) {
        value.splice(index, 1);
      }
    });
  }

  get errorMessage() {
    return this.form.errors[this.path];
  }

  get hasError() {
    return Boolean(this.errorMessage);
  }

  get label() {
    return this.schema?.spec?.label;
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

function tryReach(schema: any, prop: string) {
  try {
    return reach(schema, prop);
  } catch {
    return undefined;
  }
}
