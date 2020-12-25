export interface CreateOrchestrationRequest {
  name: string;
  version?: string;
  instanceId?: string;
  input?: any;
  tags?: Record<string, string>;
}

export interface RaiseEventRequest {
  eventName: string;
  eventData?: any;
}

export interface TerminateRequest {
  reason?: string;
}

export interface OrchestrationsResult {
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
  orchestrationStatus?: OrchestrationStatus;
  reason?: string;
  input?: string;
  result?: string;
};

export interface ParentInstance {
  orchestrationInstance: OrchestrationInstance;
  name: string;
  version: string;
  taskScheduleId: number;
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
  | "QueryCount";
