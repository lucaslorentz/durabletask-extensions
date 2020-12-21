using System;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using DurableTask.Core.Serializing;
using DurableTaskGrpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LLL.DurableTask.Core;
using LLL.DurableTask.Server.Grpc.Server.Internal;
using static DurableTaskGrpc.OrchestrationService;
using TaskActivityWorkItem = DurableTask.Core.TaskActivityWorkItem;

namespace LLL.DurableTask.Server.Grpc.Server
{
    public class GrpcServerOrchestrationService : OrchestrationServiceBase
    {
        private readonly DataConverter _dataConverter = new GrpcJsonDataConverter();

        private readonly IOrchestrationService _orchestrationService;
        private readonly IOrchestrationServiceClient _orchestrationServiceClient;
        private readonly IExtendedOrchestrationService _extendedOrchestrationService;
        private readonly IExtendedOrchestrationServiceClient _extendedOrchestrationServiceClient;

        public GrpcServerOrchestrationService(
            IOrchestrationService orchestrationService,
            IOrchestrationServiceClient orchestrationServiceClient,
            IExtendedOrchestrationService distributedOrchestrationService = null,
            IExtendedOrchestrationServiceClient extendedOrchestrationServiceClient = null)
        {
            _orchestrationService = orchestrationService;
            _orchestrationServiceClient = orchestrationServiceClient;
            _extendedOrchestrationService = distributedOrchestrationService;
            _extendedOrchestrationServiceClient = extendedOrchestrationServiceClient;
        }

        public override async Task<GetFeaturesResponse> GetFeatures(Empty request, ServerCallContext context)
        {
            var features = _extendedOrchestrationServiceClient != null
                ? await _extendedOrchestrationServiceClient.GetFeatures()
                : new OrchestrationFeature[0];

            return new GetFeaturesResponse
            {
                Features = { features.Select(f => (int)f) }
            };
        }

        public override async Task<Empty> CreateTaskOrchestration(CreateTaskOrchestrationRequest request, ServerCallContext context)
        {
            var creationMessage = _dataConverter.Deserialize<TaskMessage>(request.CreationMessage);
            var dedupeStatuses = request.DedupeStatuses.Count > 0
                ? request.DedupeStatuses.Select(x => (OrchestrationStatus)x).ToArray()
                : null;

            await _orchestrationServiceClient.CreateTaskOrchestrationAsync(creationMessage, dedupeStatuses);

            return new Empty();
        }

        public override async Task<Empty> ForceTerminateTaskOrchestration(ForceTerminateTaskOrchestrationRequest request, ServerCallContext context)
        {
            await _orchestrationServiceClient.ForceTerminateTaskOrchestrationAsync(request.InstanceId, request.Reason);

            return new Empty();
        }

        public override async Task<GetOrchestrationHistoryResponse> GetOrchestrationHistory(GetOrchestrationHistoryRequest request, ServerCallContext context)
        {
            var history = await _orchestrationServiceClient.GetOrchestrationHistoryAsync(
                request.InstanceId,
                request.ExecutionId);

            var response = new GetOrchestrationHistoryResponse
            {
                History = history
            };

            return response;
        }

        public override async Task<GetOrchestrationInstanceStateResponse> GetOrchestrationInstanceState(GetOrchestrationInstanceStateRequest request, ServerCallContext context)
        {
            var states = await _orchestrationServiceClient.GetOrchestrationStateAsync(
                request.InstanceId,
                request.AllExecutions);

            var response = new GetOrchestrationInstanceStateResponse
            {
                States = { states.Select(s => _dataConverter.Serialize(s)) }
            };

            return response;
        }

        public override async Task<GetOrchestrationStateResponse> GetOrchestrationState(GetOrchestrationStateRequest request, ServerCallContext context)
        {
            var state = await _orchestrationServiceClient.GetOrchestrationStateAsync(
                request.InstanceId,
                request.ExecutionId);

            var response = new GetOrchestrationStateResponse
            {
                State = _dataConverter.Serialize(state)
            };

            return response;
        }

        public override async Task<Empty> PurgeOrchestrationHistory(PurgeOrchestrationHistoryRequest request, ServerCallContext context)
        {
            await _orchestrationServiceClient.PurgeOrchestrationHistoryAsync(
                request.ThresholdDateTimeUtc.ToDateTime(),
                (OrchestrationStateTimeRangeFilterType)request.TimeRangeFilterType);

            return new Empty();
        }

        public override async Task<WaitForOrchestrationResponse> WaitForOrchestration(WaitForOrchestrationRequest request, ServerCallContext context)
        {
            var state = await _orchestrationServiceClient.WaitForOrchestrationAsync(
                request.InstanceId,
                request.ExecutionId,
                request.Timeout.ToTimeSpan(),
                context.CancellationToken);

            var response = new WaitForOrchestrationResponse
            {
                State = _dataConverter.Serialize(state)
            };

            return response;
        }

        public override async Task<GetOrchestrationsResponse> GetOrchestrations(GetOrchestrationsRequest request, ServerCallContext context)
        {
            var query = new OrchestrationQuery
            {
                Top = request.Top,
                ContinuationToken = request.ContinuationToken,
                InstanceId = request.InstanceId,
                Name = request.Name,
                CreatedTimeFrom = request.CreatedTimeFrom?.ToDateTime(),
                CreatedTimeTo = request.CreatedTimeTo?.ToDateTime(),
                RuntimeStatus = request.RuntimeStatus.Select(s => (OrchestrationStatus)s).ToArray()
            };

            var queryResult = await _extendedOrchestrationServiceClient.GetOrchestrationsAsync(query, context.CancellationToken);

            var response = new GetOrchestrationsResponse
            {
                States = { queryResult.Orchestrations.Select(s => _dataConverter.Serialize(s)) },
                Count = queryResult.Count,
                ContinuationToken = queryResult.ContinuationToken ?? string.Empty
            };

            return response;
        }

        public override async Task<PurgeInstanceHistoryResponse> PurgeInstanceHistory(PurgeInstanceHistoryRequest request, ServerCallContext context)
        {
            var result = await _extendedOrchestrationServiceClient.PurgeInstanceHistoryAsync(request.InstanceId);

            return new PurgeInstanceHistoryResponse
            {
                InstancesDeleted = result.InstancesDeleted
            };
        }

        public override async Task<Empty> SendTaskOrchestrationMessageBatch(SendTaskOrchestrationMessageBatchRequest request, ServerCallContext context)
        {
            var messages = request.Messages.Select(x => _dataConverter.Deserialize<TaskMessage>(x)).ToArray();

            await _orchestrationServiceClient.SendTaskOrchestrationMessageBatchAsync(messages);

            return new Empty();
        }

        public override async Task LockNextTaskOrchestrationWorkItem(IAsyncStreamReader<TaskOrchestrationRequest> requestStream, IServerStreamWriter<TaskOrchestrationResponse> responseStream, ServerCallContext context)
        {
            global::DurableTask.Core.TaskOrchestrationWorkItem workItem = null;

            // Receive and reply each message
            await foreach (var message in requestStream.ReadAllAsync())
            {
                switch (message.MessageCase)
                {
                    case TaskOrchestrationRequest.MessageOneofCase.LockRequest:
                        var lockRequest = message.LockRequest;
                        var orchestrations = lockRequest.Orchestrations.Select(x => new NameVersion(x.Name, x.Version)).ToArray();

                        workItem = lockRequest.AllOrchestrations switch
                        {
                            false => await (_extendedOrchestrationService ?? throw new NotSupportedException("Distributed orchestration is not supported"))
                                .LockNextTaskOrchestrationWorkItemAsync(
                                    lockRequest.ReceiveTimeout.ToTimeSpan(),
                                    orchestrations,
                                    context.CancellationToken),
                            true => await _orchestrationService.LockNextTaskOrchestrationWorkItemAsync(
                                lockRequest.ReceiveTimeout.ToTimeSpan(),
                                context.CancellationToken)
                        };

                        var lockResponse = new TaskOrchestrationResponse
                        {
                            LockResponse = new LockNextTaskOrchestrationWorkItemResponse
                            {
                                WorkItem = workItem == null ? null : new DurableTaskGrpc.TaskOrchestrationWorkItem
                                {
                                    InstanceId = workItem.InstanceId,
                                    LockedUntilUtc = Timestamp.FromDateTime(workItem.LockedUntilUtc),
                                    Events = { workItem.OrchestrationRuntimeState.Events.Select(_dataConverter.Serialize) },
                                    NewMessages = { workItem.NewMessages.Select(_dataConverter.Serialize) }
                                }
                            }
                        };

                        await responseStream.WriteAsync(lockResponse);
                        break;
                    case TaskOrchestrationRequest.MessageOneofCase.RenewRequest:
                        var renewRequest = message.RenewRequest;
                        await _orchestrationService.RenewTaskOrchestrationWorkItemLockAsync(workItem);

                        var renewResponse = new TaskOrchestrationResponse
                        {
                            RenewResponse = new RenewTaskOrchestrationWorkItemLockResponse
                            {
                                LockedUntilUtc = Timestamp.FromDateTime(workItem.LockedUntilUtc)
                            }
                        };

                        await responseStream.WriteAsync(renewResponse);
                        break;
                    case TaskOrchestrationRequest.MessageOneofCase.CompleteRequest:
                        var completeRequest = message.CompleteRequest;
                        var outboundMessages = completeRequest.OutboundMessages.Select(x => _dataConverter.Deserialize<TaskMessage>(x)).ToArray();
                        var timerMessages = completeRequest.TimerMessages.Select(x => _dataConverter.Deserialize<TaskMessage>(x)).ToArray();
                        var orchestratorMessages = completeRequest.OrchestratorMessages.Select(x => _dataConverter.Deserialize<TaskMessage>(x)).ToArray();
                        var continuedAsNewMessage = _dataConverter.Deserialize<TaskMessage>(completeRequest.ContinuedAsNewMessage);
                        var orchestrationState = _dataConverter.Deserialize<OrchestrationState>(completeRequest.OrchestrationState);

                        var newEvents = completeRequest.NewEvents.Select(x => _dataConverter.Deserialize<HistoryEvent>(x)).ToArray();

                        var executionStartedEvent = newEvents
                            .OfType<ExecutionStartedEvent>()
                            .FirstOrDefault();

                        var isNewExecution = executionStartedEvent != null
                            && workItem.OrchestrationRuntimeState != null
                            && workItem.OrchestrationRuntimeState.OrchestrationInstance != null
                            && executionStartedEvent.OrchestrationInstance.ExecutionId != workItem.OrchestrationRuntimeState.OrchestrationInstance.ExecutionId;

                        if (isNewExecution)
                        {
                            workItem.OrchestrationRuntimeState = new OrchestrationRuntimeState();
                        }

                        foreach (var newEvent in newEvents)
                            workItem.OrchestrationRuntimeState.AddEvent(newEvent);

                        await _orchestrationService.CompleteTaskOrchestrationWorkItemAsync(
                            workItem,
                            workItem.OrchestrationRuntimeState,
                            outboundMessages,
                            orchestratorMessages,
                            timerMessages,
                            continuedAsNewMessage,
                            orchestrationState);

                        workItem.OrchestrationRuntimeState.NewEvents.Clear();

                        await responseStream.WriteAsync(new TaskOrchestrationResponse
                        {
                            CompleteResponse = new CompleteTaskOrchestrationWorkItemResponse()
                        });
                        break;
                    case TaskOrchestrationRequest.MessageOneofCase.FetchRequest:
                        var fetchRequest = message.FetchRequest;
                        if (workItem.Session == null)
                        {
                            var fetchResponse = new TaskOrchestrationResponse
                            {
                                FetchResponse = new FetchNewOrchestrationMessagesResponse
                                {
                                    NewMessages = null
                                }
                            };

                            await responseStream.WriteAsync(fetchResponse);
                        }
                        else
                        {
                            var newMessages = await workItem.Session.FetchNewOrchestrationMessagesAsync(workItem);

                            var fetchResponse = new TaskOrchestrationResponse
                            {
                                FetchResponse = new FetchNewOrchestrationMessagesResponse
                                {
                                    NewMessages = newMessages == null ? null : new OrchestrationMessages
                                    {
                                        Messages = { newMessages.Select(_dataConverter.Serialize) }
                                    }
                                }
                            };

                            await responseStream.WriteAsync(fetchResponse);
                        }
                        break;
                    case TaskOrchestrationRequest.MessageOneofCase.ReleaseRequest:
                        var releaseRequest = message.ReleaseRequest;
                        await _orchestrationService.ReleaseTaskOrchestrationWorkItemAsync(workItem);
                        await responseStream.WriteAsync(new TaskOrchestrationResponse
                        {
                            ReleaseResponse = new ReleaseTaskOrchestrationWorkItemResponse()
                        });
                        break;
                    case TaskOrchestrationRequest.MessageOneofCase.AbandonRequest:
                        var abandonRequest = message.AbandonRequest;
                        await _orchestrationService.AbandonTaskOrchestrationWorkItemAsync(workItem);
                        await responseStream.WriteAsync(new TaskOrchestrationResponse
                        {
                            AbandonResponse = new AbandonTaskOrchestrationWorkItemLockResponse()
                        });
                        break;
                }
            }
        }

        public override async Task<LockNextTaskActivityWorkItemResponse> LockNextTaskActivityWorkItem(LockNextTaskActivityWorkItemRequest request, ServerCallContext context)
        {
            var activities = request.Activities.Select(x => new NameVersion(x.Name, x.Version)).ToArray();

            var workItem = request.AllActivities switch
            {
                false => await (_extendedOrchestrationService ?? throw new NotSupportedException("Distributed activity is not supported"))
                    .LockNextTaskActivityWorkItem(
                        request.ReceiveTimeout.ToTimeSpan(),
                        activities,
                        context.CancellationToken),
                true => await _orchestrationService.LockNextTaskActivityWorkItem(
                    request.ReceiveTimeout.ToTimeSpan(),
                    context.CancellationToken)
            };

            var response = new LockNextTaskActivityWorkItemResponse
            {
                WorkItem = workItem == null ? null : ToGrpcWorkItem(workItem)
            };

            return response;
        }

        public override async Task<RenewTaskActivityWorkItemLockResponse> RenewTaskActivityWorkItemLock(RenewTaskActivityWorkItemLockRequest request, ServerCallContext context)
        {
            var workItem = ToDurableTaskWorkItem(request.WorkItem);

            var newWorkItem = await _orchestrationService.RenewTaskActivityWorkItemLockAsync(workItem);

            var response = new RenewTaskActivityWorkItemLockResponse
            {
                WorkItem = ToGrpcWorkItem(newWorkItem)
            };

            return response;
        }

        public override async Task<Empty> CompleteTaskActivityWorkItem(
            CompleteTaskActivityWorkItemRequest request, ServerCallContext context)
        {
            var workItem = ToDurableTaskWorkItem(request.WorkItem);

            var responseMessage = _dataConverter.Deserialize<TaskMessage>(request.ResponseMessage);

            await _orchestrationService.CompleteTaskActivityWorkItemAsync(workItem, responseMessage);

            return new Empty();
        }

        public override async Task<Empty> AbandonTaskActivityWorkItem(AbandonTaskActivityWorkItemRequest request, ServerCallContext context)
        {
            var workItem = ToDurableTaskWorkItem(request.WorkItem);

            await _orchestrationService.AbandonTaskActivityWorkItemAsync(workItem);

            return new Empty();
        }

        private DurableTaskGrpc.TaskActivityWorkItem ToGrpcWorkItem(TaskActivityWorkItem workItem)
        {
            return new DurableTaskGrpc.TaskActivityWorkItem
            {
                Id = workItem.Id,
                LockedUntilUtc = Timestamp.FromDateTime(workItem.LockedUntilUtc),
                TaskMessage = _dataConverter.Serialize(workItem.TaskMessage)
            };
        }

        private TaskActivityWorkItem ToDurableTaskWorkItem(DurableTaskGrpc.TaskActivityWorkItem grpcWorkItem)
        {
            return new TaskActivityWorkItem
            {
                Id = grpcWorkItem.Id,
                LockedUntilUtc = grpcWorkItem.LockedUntilUtc.ToDateTime(),
                TaskMessage = _dataConverter.Deserialize<TaskMessage>(grpcWorkItem.TaskMessage)
            };
        }
    }
}
