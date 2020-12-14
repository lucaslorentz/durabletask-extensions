import "yup";

declare module "yup" {
  interface StringSchema {
    json(): StringSchema;
  }
  interface Schema<T> {
    formRender(field: FormField<T>): Schema<T>;
  }
}
