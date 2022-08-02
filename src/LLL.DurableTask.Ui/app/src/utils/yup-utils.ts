import { TestContext } from "yup";

export function validateJson(value: string, testContext: TestContext) {
  if (!value) return true;
  try {
    JSON.parse(value);
    return true;
  } catch (e) {
    return testContext.createError({
      path: testContext.path,
      message: `Invalid JSON: ${e}`,
    });
  }
}
