export interface CreateOrchestrationRequest {
  name: string;
  version?: string;
  instanceId?: string;
  input?: any;
  tags?: Record<string, string>;
}

export interface TerminateRequest {
  reason?: string;
}

export interface RewindRequest {
  reason?: string;
}

export interface OrchestrationsRequest {
  instanceId?: string;
  name?: string;
  createdTimeFrom?: string;
  createdTimeTo?: string;
  runtimeStatus?: OrchestrationStatus[];
  includePreviousExecutions?: boolean;
  top?: number;
  continuationToken?: string;
}

export interface OrchestrationsResponse {
  orchestrations: OrchestrationState[];
  count: number;
  continuationToken: string;
}

export interface OrchestrationState {
  orchestrationInstance: OrchestrationInstance;
  name: string;
  version: string;
  createdTime: string;
  lastUpdatedTime: string;
  completedTime: string;
  parentInstance?: ParentInstance;
  tags: Record<string, string>;
  orchestrationStatus: OrchestrationStatus;
  status: string;
  input: string;
  output: string;
}

export interface OrchestrationInstance {
  instanceId: string;
  executionId: string;
}

export type HistoryEvent = {
  timestamp: string;
  eventType: string;
  eventId: number;
  isPlayed: boolean;
  orchestrationInstance?: OrchestrationInstance;
  parentInstance?: ParentInstance;
  name?: string;
  version?: string;
  tags?: Record<string, string>;
  correlation?: string;
  scheduledStartTime?: string;
  taskScheduledId?: number;
  instanceId?: string;
  fireAt?: string;
  reason?: string;
  input?: string;
  data?: string;
  result?: string;
  orchestrationStatus?: OrchestrationStatus;
};

export interface ParentInstance {
  orchestrationInstance: OrchestrationInstance;
  name: string;
  version: string;
  taskScheduleId: number;
}

export interface EntrypointResponse {
  features: Feature[];
  endpoints: Record<Endpoint, EndpointInfo>;
}

export interface EndpointInfo {
  href: string;
  method: string;
  authorized: boolean;
}

export type OrchestrationStatus =
  | "Running"
  | "Completed"
  | "ContinuedAsNew"
  | "Failed"
  | "Canceled"
  | "Terminated"
  | "Pending";

export type Feature =
  | "SearchByInstanceId"
  | "SearchByName"
  | "SearchByCreatedTime"
  | "SearchByStatus"
  | "QueryCount"
  | "Rewind"
  | "Tags"
  | "StatePerExecution";

export type Endpoint =
  | "Entrypoint"
  | "OrchestrationsList"
  | "OrchestrationsCreate"
  | "OrchestrationsGet"
  | "OrchestrationsGetExecution"
  | "OrchestrationsGetExecutionHistory"
  | "OrchestrationsTerminate"
  | "OrchestrationsRewind"
  | "OrchestrationsRaiseEvent"
  | "OrchestrationsPurgeInstance";
