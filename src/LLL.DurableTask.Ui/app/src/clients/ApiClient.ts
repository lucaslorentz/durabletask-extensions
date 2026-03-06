import qs from "qs";
import {
  CreateOrchestrationRequest,
  Endpoint,
  EndpointInfo,
  EntrypointResponse,
  Feature,
  HistoryEvent,
  OrchestrationInstance,
  OrchestrationsRequest,
  OrchestrationsResponse,
  OrchestrationState,
  RewindRequest,
  TerminateRequest,
} from "../models/ApiModels";

export class ApiClient {
  private baseURL = "";
  private token?: string;
  private features?: Partial<Record<Feature, true>>;
  private endpoints?: Record<Endpoint, EndpointInfo>;

  public setToken(token: string | undefined) {
    this.token = token;
  }

  public async initialize(apiBaseUrl: string) {
    this.baseURL = apiBaseUrl;

    const entrypoint = await this.get<EntrypointResponse>("/");

    this.features = entrypoint.features.reduce((c, f) => {
      c[f] = true;
      return c;
    }, {} as Partial<Record<Feature, true>>);

    this.endpoints = entrypoint.endpoints;
  }

  public hasFeature(feature: Feature): boolean {
    if (!this.features) throw new Error("ApiClient not initialized");

    return Boolean(this.features[feature]);
  }

  public isAuthorized(endpoint: Endpoint): boolean {
    if (!this.endpoints) throw new Error("ApiClient not initialized");

    return Boolean(this.endpoints?.[endpoint].authorized);
  }

  public async listOrchestrations(
    request?: OrchestrationsRequest
  ): Promise<OrchestrationsResponse> {
    const query = qs.stringify(request, {
      skipNulls: true,
      arrayFormat: "repeat",
      filter(prefix: string, value: unknown) {
        if (prefix && value?.constructor === Object) {
          return JSON.stringify(value);
        } else {
          return value ? value : undefined;
        }
      },
    });

    return this.get<OrchestrationsResponse>(`/v1/orchestrations?${query}`);
  }

  public async getOrchestrationState(
    instanceId: string,
    executionId?: string
  ): Promise<OrchestrationState> {
    let url = `/v1/orchestrations/${encodeURIComponent(instanceId)}`;

    if (executionId) {
      url = `${url}/${executionId}`;
    }

    return this.get<OrchestrationState>(url);
  }

  public async getOrchestrationHistory(
    instanceId: string,
    executionId: string
  ): Promise<HistoryEvent[]> {
    return this.get<HistoryEvent[]>(
      `/v1/orchestrations/${encodeURIComponent(
        instanceId
      )}/${encodeURIComponent(executionId)}/history`
    );
  }

  public async createOrchestration(
    request: CreateOrchestrationRequest
  ): Promise<OrchestrationInstance> {
    return this.post<OrchestrationInstance>(`/v1/orchestrations`, request);
  }

  public async terminateOrchestration(
    instanceId: string,
    request: TerminateRequest
  ): Promise<void> {
    await this.post(
      `/v1/orchestrations/${encodeURIComponent(instanceId)}/terminate`,
      request
    );
  }

  public async rewindOrchestration(
    instanceId: string,
    request: RewindRequest
  ): Promise<void> {
    await this.post(
      `/v1/orchestrations/${encodeURIComponent(instanceId)}/rewind`,
      request
    );
  }

  public async purgeOrchestration(instanceId: string): Promise<void> {
    await this.fetch(
      `/v1/orchestrations/${encodeURIComponent(instanceId)}`,
      { method: "DELETE" }
    );
  }

  public async raiseOrchestrationEvent(
    instanceId: string,
    eventName: string,
    eventData: unknown
  ): Promise<void> {
    await this.post(
      `/v1/orchestrations/${encodeURIComponent(
        instanceId
      )}/raiseevent/${eventName}`,
      eventData
    );
  }

  private async get<T>(path: string): Promise<T> {
    const response = await this.fetch(path);
    return response.json();
  }

  private async post<T = void>(path: string, body?: unknown): Promise<T> {
    const response = await this.fetch(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const text = await response.text();
    return text ? JSON.parse(text) : (undefined as T);
  }

  private async fetch(path: string, init?: RequestInit): Promise<Response> {
    const headers = new Headers(init?.headers);
    if (this.token) {
      headers.set("Authorization", `Bearer ${this.token}`);
    }

    const response = await fetch(`${this.baseURL}${path}`, {
      ...init,
      headers,
    });

    if (!response.ok) {
      throw new ApiError(response.status, await response.text());
    }

    return response;
  }
}

export class ApiError extends Error {
  constructor(public status: number, body: string) {
    super(`HTTP ${status}: ${body}`);
  }
}
