import * as yup from "yup";

yup.addMethod<yup.StringSchema>(yup.string, "json", function () {
  return this.test("JSON", "Must be a valid json", function (v) {
    if (!v) return true;
    try {
      JSON.parse(v);
      return true;
    } catch (e) {
      return this.createError({
        path: this.path,
        message: `Invalid JSON: ${e}`,
      });
    }
  });
});

yup.addMethod<yup.StringSchema>(yup.string, "formRender", function (formRender) {
  var next = this.clone() as any;
  next.formRender = formRender;
  return next;
});