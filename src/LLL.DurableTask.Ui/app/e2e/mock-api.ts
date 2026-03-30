import { Page } from "@playwright/test";

const ENTRYPOINT_RESPONSE = {
  features: [
    "SearchByInstanceId",
    "SearchByName",
    "SearchByCreatedTime",
    "SearchByStatus",
    "Rewind",
    "Tags",
    "StatePerExecution",
  ],
  endpoints: {
    Entrypoint: { href: "/api/", method: "GET", authorized: true },
    OrchestrationsList: {
      href: "/api/v1/orchestrations",
      method: "GET",
      authorized: true,
    },
    OrchestrationsCreate: {
      href: "/api/v1/orchestrations",
      method: "POST",
      authorized: true,
    },
    OrchestrationsGet: {
      href: "/api/v1/orchestrations/{instanceId}",
      method: "GET",
      authorized: true,
    },
    OrchestrationsGetExecution: {
      href: "/api/v1/orchestrations/{instanceId}/{executionId}",
      method: "GET",
      authorized: true,
    },
    OrchestrationsGetExecutionHistory: {
      href: "/api/v1/orchestrations/{instanceId}/{executionId}/history",
      method: "GET",
      authorized: true,
    },
    OrchestrationsTerminate: {
      href: "/api/v1/orchestrations/{instanceId}/terminate",
      method: "POST",
      authorized: true,
    },
    OrchestrationsRewind: {
      href: "/api/v1/orchestrations/{instanceId}/rewind",
      method: "POST",
      authorized: true,
    },
    OrchestrationsRaiseEvent: {
      href: "/api/v1/orchestrations/{instanceId}/raiseevent/{eventName}",
      method: "POST",
      authorized: true,
    },
    OrchestrationsPurgeInstance: {
      href: "/api/v1/orchestrations/{instanceId}",
      method: "DELETE",
      authorized: true,
    },
  },
};

const SAMPLE_ORCHESTRATIONS = {
  orchestrationState: [
    {
      orchestrationInstance: {
        instanceId: "test-instance-1",
        executionId: "exec-001",
      },
      name: "SampleOrchestration",
      version: "1.0",
      createdTime: "2026-03-28T10:00:00Z",
      lastUpdatedTime: "2026-03-28T10:05:00Z",
      completedTime: "2026-03-28T10:05:00Z",
      tags: {},
      orchestrationStatus: "Completed",
      status: "",
      input: '"hello"',
      output: '"world"',
    },
    {
      orchestrationInstance: {
        instanceId: "test-instance-2",
        executionId: "exec-002",
      },
      name: "RunningOrchestration",
      version: "1.0",
      createdTime: "2026-03-29T08:00:00Z",
      lastUpdatedTime: "2026-03-29T08:01:00Z",
      completedTime: "",
      tags: { env: "test" },
      orchestrationStatus: "Running",
      status: "",
      input: '{"key":"value"}',
      output: "",
    },
  ],
  continuationToken: "",
};

const SAMPLE_HISTORY = [
  {
    timestamp: "2026-03-28T10:00:00Z",
    eventType: "ExecutionStarted",
    eventId: -1,
    isPlayed: true,
    name: "SampleOrchestration",
    version: "1.0",
    input: '"hello"',
  },
  {
    timestamp: "2026-03-28T10:01:00Z",
    eventType: "TaskScheduled",
    eventId: 0,
    isPlayed: true,
    name: "SayHello",
    version: "",
  },
  {
    timestamp: "2026-03-28T10:02:00Z",
    eventType: "TaskCompleted",
    eventId: -1,
    isPlayed: true,
    taskScheduledId: 0,
    result: '"Hello, World!"',
  },
  {
    timestamp: "2026-03-28T10:05:00Z",
    eventType: "ExecutionCompleted",
    eventId: -1,
    isPlayed: true,
    orchestrationStatus: "Completed",
    result: '"world"',
  },
];

const CONFIGURATION = {
  apiBaseUrl: "/api",
};

export async function mockApi(page: Page) {
  // Mock configuration.json
  await page.route("**/configuration.json", (route) =>
    route.fulfill({ json: CONFIGURATION })
  );

  // Mock API entrypoint
  await page.route("**/api/", (route) =>
    route.fulfill({ json: ENTRYPOINT_RESPONSE })
  );

  // Mock orchestrations list
  await page.route("**/api/v1/orchestrations?**", (route) =>
    route.fulfill({ json: SAMPLE_ORCHESTRATIONS })
  );
  await page.route("**/api/v1/orchestrations", (route) => {
    if (route.request().method() === "GET") {
      return route.fulfill({ json: SAMPLE_ORCHESTRATIONS });
    }
    // POST = create orchestration
    return route.fulfill({
      json: { instanceId: "new-instance", executionId: "new-exec" },
    });
  });

  // Mock single orchestration
  await page.route(
    "**/api/v1/orchestrations/test-instance-1",
    (route) =>
      route.fulfill({
        json: SAMPLE_ORCHESTRATIONS.orchestrationState[0],
      })
  );
  await page.route(
    "**/api/v1/orchestrations/test-instance-1/exec-001",
    (route) =>
      route.fulfill({
        json: SAMPLE_ORCHESTRATIONS.orchestrationState[0],
      })
  );

  // Mock history
  await page.route("**/api/v1/orchestrations/*/history", (route) =>
    route.fulfill({ json: SAMPLE_HISTORY })
  );
  await page.route("**/api/v1/orchestrations/*/*/history", (route) =>
    route.fulfill({ json: SAMPLE_HISTORY })
  );

  // Mock terminate/rewind/purge/raiseevent
  await page.route("**/api/v1/orchestrations/*/terminate", (route) =>
    route.fulfill({ status: 200 })
  );
  await page.route("**/api/v1/orchestrations/*/rewind", (route) =>
    route.fulfill({ status: 200 })
  );
  await page.route("**/api/v1/orchestrations/*/raiseevent/**", (route) =>
    route.fulfill({ status: 200 })
  );
}
