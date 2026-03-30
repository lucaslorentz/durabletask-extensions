import { test, expect } from "@playwright/test";
import { mockApi } from "./mock-api";

test.beforeEach(async ({ page }) => {
  await mockApi(page);
});

test.describe("Home", () => {
  test("loads and shows navigation", async ({ page }) => {
    await page.goto("/");
    await expect(
      page.getByRole("heading", { name: "Welcome to Durable Task UI" })
    ).toBeVisible();
    await expect(page.getByRole("link", { name: "Orchestrations" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Create" })).toBeVisible();
  });
});

test.describe("Orchestrations list", () => {
  test("renders orchestrations table", async ({ page }) => {
    await page.goto("/#/orchestrations");
    await expect(page.locator("text=SampleOrchestration")).toBeVisible();
    await expect(page.locator("text=RunningOrchestration")).toBeVisible();
  });

  test("shows status for each orchestration", async ({ page }) => {
    await page.goto("/#/orchestrations");
    await expect(page.getByRole("cell", { name: "Completed", exact: true })).toBeVisible();
    await expect(page.getByRole("cell", { name: "Running", exact: true })).toBeVisible();
  });

  test("has search section", async ({ page }) => {
    await page.goto("/#/orchestrations");
    await expect(
      page.getByRole("button", { name: "Search" })
    ).toBeVisible();
  });

  test("shows table headers", async ({ page }) => {
    await page.goto("/#/orchestrations");
    await expect(page.getByRole("columnheader", { name: "InstanceId" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Name" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Status" })).toBeVisible();
  });

  test("shows tags in table", async ({ page }) => {
    await page.goto("/#/orchestrations");
    await expect(page.locator("text=env: test")).toBeVisible();
  });
});

test.describe("Orchestration detail", () => {
  test("shows orchestration state", async ({ page }) => {
    await page.goto("/#/orchestrations/test-instance-1");
    await expect(page.locator("text=test-instance-1").first()).toBeVisible();
    await expect(page.locator("text=SampleOrchestration")).toBeVisible();
  });

  test("shows state tab with details", async ({ page }) => {
    await page.goto("/#/orchestrations/test-instance-1");
    await expect(page.getByRole("tab", { name: "State" })).toBeVisible();
    await expect(page.getByRole("cell", { name: "Completed", exact: true })).toBeVisible();
    await expect(page.getByRole("cell", { name: "exec-001" })).toBeVisible();
  });

  test("has action buttons", async ({ page }) => {
    await page.goto("/#/orchestrations/test-instance-1");
    await expect(page.getByRole("button", { name: "Terminate" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Rewind" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Purge" })).toBeVisible();
  });

  test("has history tab", async ({ page }) => {
    await page.goto("/#/orchestrations/test-instance-1");
    const historyTab = page.getByRole("tab", { name: "History" });
    await expect(historyTab).toBeVisible();
    await historyTab.click();
    await expect(page.locator("text=ExecutionStarted")).toBeVisible();
  });

  test("has executions tab", async ({ page }) => {
    await page.goto("/#/orchestrations/test-instance-1");
    await expect(page.getByRole("tab", { name: "Executions" })).toBeVisible();
  });

  test("has raise event tab", async ({ page }) => {
    await page.goto("/#/orchestrations/test-instance-1");
    await expect(page.getByRole("tab", { name: "Raise Event" })).toBeVisible();
  });
});

test.describe("Create orchestration", () => {
  test("renders create form with fields", async ({ page }) => {
    await page.goto("/#/create");
    await expect(page.getByLabel("Name")).toBeVisible();
    await expect(page.getByLabel("Version")).toBeVisible();
    await expect(page.getByLabel("Instance Id")).toBeVisible();
  });

  test("create button is disabled when form is invalid", async ({ page }) => {
    await page.goto("/#/create");
    // Button should be disabled because Name is required and empty
    const createButton = page.getByRole("button", { name: "Create" });
    await expect(createButton).toBeDisabled();
  });

  test("has add tag button", async ({ page }) => {
    await page.goto("/#/create");
    await expect(page.getByRole("button", { name: "Add tag" })).toBeVisible();
  });

  test("has reset button", async ({ page }) => {
    await page.goto("/#/create");
    await expect(page.getByRole("button", { name: "Reset" })).toBeVisible();
  });
});

test.describe("Navigation", () => {
  test("navigates from home to orchestrations", async ({ page }) => {
    await page.goto("/");
    await page.getByRole("link", { name: "Orchestrations" }).click();
    await expect(page).toHaveURL(/orchestrations/);
    await expect(page.locator("text=SampleOrchestration")).toBeVisible();
  });

  test("navigates from home to create", async ({ page }) => {
    await page.goto("/");
    await page.getByRole("link", { name: "Create" }).click();
    await expect(page).toHaveURL(/create/);
    await expect(page.getByLabel("Name")).toBeVisible();
  });

  test("navigates from list to detail", async ({ page }) => {
    await page.goto("/#/orchestrations");
    await page.getByRole("link", { name: "test-instance-1" }).click();
    await expect(page).toHaveURL(/test-instance-1/);
    await expect(page.locator("text=SampleOrchestration")).toBeVisible();
  });

  test("breadcrumb navigation works", async ({ page }) => {
    await page.goto("/#/orchestrations/test-instance-1");
    await page.getByLabel("breadcrumb").getByRole("link", { name: "Orchestrations" }).click();
    await expect(page).toHaveURL(/orchestrations/);
    await expect(page.locator("text=SampleOrchestration")).toBeVisible();
  });
});

test.describe("Error handling", () => {
  test("shows error when API fails", async ({ page }) => {
    // Override the orchestrations mock to return 500
    await page.route("**/api/v1/orchestrations?**", (route) =>
      route.fulfill({ status: 500, body: "Internal Server Error" })
    );
    await page.route("**/api/v1/orchestrations", (route) => {
      if (route.request().method() === "GET") {
        return route.fulfill({ status: 500, body: "Internal Server Error" });
      }
      return route.continue();
    });

    await page.goto("/#/orchestrations");
    await expect(
      page.locator("text=/error|500|failed/i").first()
    ).toBeVisible({ timeout: 10_000 });
  });
});
