import axios, { AxiosInstance } from "axios";
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
  private apiAxios: AxiosInstance;
  private features?: Partial<Record<Feature, true>>;
  private endpoints?: Record<Endpoint, EndpointInfo>;

  constructor() {
    this.apiAxios = axios.create();
  }

  public setToken(token: string | undefined) {
    if (token) {
      this.apiAxios.defaults.headers.common.Authorization = `Bearer ${token}`;
    } else {
      delete this.apiAxios.defaults.headers.common.Authorization;
    }
  }

  public async initialize(apiBaseUrl: string) {
    this.apiAxios.defaults.baseURL = apiBaseUrl;

    var entrypointResponse = await this.apiAxios.get<EntrypointResponse>("/");

    this.features = entrypointResponse.data.features.reduce((c, f) => {
      c[f] = true;
      return c;
    }, {} as Partial<Record<Feature, true>>);

    this.endpoints = entrypointResponse.data.endpoints;
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
    const params = new URLSearchParams();
    if (request) {
      request.instanceId && params.append("instanceId", request.instanceId);
      request.name && params.append("name", request.name);
      request.createdTimeFrom &&
        params.append("createdTimeFrom", request.createdTimeFrom);
      request.createdTimeTo &&
        params.append("createdTimeTo", request.createdTimeTo);
      request.runtimeStatus?.forEach((status) =>
        params.append("runtimeStatus", status)
      );
      request.top && params.append("top", request.top.toString());
      request.continuationToken &&
        params.append("continuationToken", request.continuationToken);
    }

    var response = await this.apiAxios.get<OrchestrationsResponse>(
      `/v1/orchestrations?${params.toString()}`
    );

    return response.data;
  }

  public async getOrchestrationState(
    instanceId: string,
    executionId?: string
  ): Promise<OrchestrationState> {
    let url = `/v1/orchestrations/${encodeURIComponent(instanceId)}`;

    if (executionId) {
      url = `${url}/${executionId}`;
    }

    var response = await this.apiAxios.get<OrchestrationState>(url);
    return response.data;
  }

  public async getOrchestrationHistory(
    instanceId: string,
    executionId: string
  ): Promise<HistoryEvent[]> {
    var response = await this.apiAxios.get<HistoryEvent[]>(
      `/v1/orchestrations/${encodeURIComponent(
        instanceId
      )}/${encodeURIComponent(executionId)}/history`
    );
    return response.data;
  }

  public async createOrchestration(
    request: CreateOrchestrationRequest
  ): Promise<OrchestrationInstance> {
    const response = await this.apiAxios.post<OrchestrationInstance>(
      `/v1/orchestrations`,
      request
    );
    return response.data;
  }

  public async terminateOrchestration(
    instanceId: string,
    request: TerminateRequest
  ): Promise<void> {
    await this.apiAxios.post(
      `/v1/orchestrations/${encodeURIComponent(instanceId)}/terminate`,
      request
    );
  }

  public async rewindOrchestration(
    instanceId: string,
    request: RewindRequest
  ): Promise<void> {
    await this.apiAxios.post(
      `/v1/orchestrations/${encodeURIComponent(instanceId)}/rewind`,
      request
    );
  }

  public async purgeOrchestration(instanceId: string): Promise<void> {
    await this.apiAxios.delete(
      `/v1/orchestrations/${encodeURIComponent(instanceId)}`
    );
  }

  public async raiseOrchestrationEvent(
    instanceId: string,
    eventName: string,
    eventData: any
  ): Promise<void> {
    await this.apiAxios.post(
      `/v1/orchestrations/${encodeURIComponent(
        instanceId
      )}/raiseevent/${eventName}`,
      eventData
    );
  }
}
