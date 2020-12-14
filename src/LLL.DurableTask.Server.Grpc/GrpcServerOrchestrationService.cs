using System;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using DurableTask.Core.Serializing;
using DurableTaskHub;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LLL.DurableTask.Core;
using LLL.DurableTask.Server.Grpc.Server.Internal;
using static DurableTaskHub.OrchestrationService;

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
                CountIsNull = queryResult.Count == null,
                Count = queryResult.Count ?? default,
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
            if (!await requestStream.MoveNext(context.CancellationToken))
                throw new Exception("Session closed");

            var lockRequest = requestStream.Current.LockRequest;
            if (lockRequest == null)
                throw new Exception("Expected lock request");

            var orchestrations = lockRequest.Orchestrations.Select(x => new NameVersion(x.Name, x.Version)).ToArray();

            var workItem = lockRequest.AllOrchestrations switch
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
                LockResponse = workItem == null
                    ? new LockNextTaskOrchestrationWorkItemResponse()
                    : new LockNextTaskOrchestrationWorkItemResponse
                    {
                        InstanceId = workItem.InstanceId,
                        LockedUntilUtc = Timestamp.FromDateTime(workItem.LockedUntilUtc),
                        Events = { workItem.OrchestrationRuntimeState.Events.Select(_dataConverter.Serialize) },
                        NewMessages = { workItem.NewMessages.Select(_dataConverter.Serialize) }
                    }
            };

            await responseStream.WriteAsync(lockResponse);

            while (await requestStream.MoveNext())
            {
                var renewRequest = requestStream.Current.RenewRequest;
                var fetchRequest = requestStream.Current.FetchRequest;
                var completeRequest = requestStream.Current.CompleteRequest;
                var releaseRequest = requestStream.Current.ReleaseRequest;
                var abandonRequest = requestStream.Current.AbandonRequest;

                if (renewRequest != null)
                {
                    await _orchestrationService.RenewTaskOrchestrationWorkItemLockAsync(workItem);

                    var renewResponse = new TaskOrchestrationResponse
                    {
                        RenewResponse = new RenewTaskOrchestrationWorkItemLockResponse
                        {
                            LockedUntilUtc = Timestamp.FromDateTime(workItem.LockedUntilUtc)
                        }
                    };

                    await responseStream.WriteAsync(renewResponse);
                }
                else if (completeRequest != null)
                {
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
                }
                else if (fetchRequest != null)
                {
                    if (workItem.Session == null)
                    {
                        var fetchResponse = new TaskOrchestrationResponse
                        {
                            FetchResponseIsNull = true
                        };

                        await responseStream.WriteAsync(fetchResponse);
                    }
                    else
                    {
                        var newMessages = await workItem.Session.FetchNewOrchestrationMessagesAsync(workItem);

                        if (newMessages == null)
                        {
                            var fetchResponse = new TaskOrchestrationResponse
                            {
                                FetchResponseIsNull = true
                            };

                            await responseStream.WriteAsync(fetchResponse);
                        }
                        else
                        {
                            var fetchResponse = new TaskOrchestrationResponse
                            {
                                FetchResponse = new FetchNewOrchestrationMessagesResponse
                                {
                                    NewMessages = { newMessages.Select(_dataConverter.Serialize) }
                                }
                            };

                            await responseStream.WriteAsync(fetchResponse);
                        }
                    }
                }
                else if (releaseRequest != null)
                {
                    await _orchestrationService.ReleaseTaskOrchestrationWorkItemAsync(workItem);

                    await responseStream.WriteAsync(new TaskOrchestrationResponse
                    {
                        ReleaseResponse = new ReleaseTaskOrchestrationWorkItemResponse()
                    });

                    break;
                }
                else if (abandonRequest != null)
                {
                    await _orchestrationService.AbandonTaskOrchestrationWorkItemAsync(workItem);

                    await responseStream.WriteAsync(new TaskOrchestrationResponse
                    {
                        AbandonResponse = new AbandonTaskOrchestrationWorkItemLockResponse()
                    });

                    break;
                }

                context.CancellationToken.ThrowIfCancellationRequested();
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
                IsNull = workItem == null,
                Value = workItem == null ? null : new LockNextTaskActivityWorkItemResponseValue
                {
                    Id = workItem.Id,
                    LockedUntilUtc = Timestamp.FromDateTime(workItem.LockedUntilUtc),
                    TaskMessage = _dataConverter.Serialize(workItem.TaskMessage)
                }
            };

            return response;
        }

        public override async Task<RenewTaskActivityWorkItemLockResponse> RenewTaskActivityWorkItemLock(RenewTaskActivityWorkItemLockRequest request, ServerCallContext context)
        {
            var workItem = new TaskActivityWorkItem
            {
                Id = request.Id,
                LockedUntilUtc = request.LockedUntilUtc.ToDateTime(),
                TaskMessage = _dataConverter.Deserialize<TaskMessage>(request.TaskMessage)
            };

            var newWorkItem = await _orchestrationService.RenewTaskActivityWorkItemLockAsync(workItem);

            var response = new RenewTaskActivityWorkItemLockResponse
            {
                Id = newWorkItem.Id,
                LockedUntilUtc = Timestamp.FromDateTime(newWorkItem.LockedUntilUtc),
                TaskMessage = _dataConverter.Serialize(newWorkItem.TaskMessage)
            };

            return response;
        }

        public override async Task<Empty> CompleteTaskActivityWorkItem(
            CompleteTaskActivityWorkItemRequest request, ServerCallContext context)
        {
            var workItem = new TaskActivityWorkItem
            {
                Id = request.Id,
                LockedUntilUtc = request.LockedUntilUtc.ToDateTime(),
                TaskMessage = _dataConverter.Deserialize<TaskMessage>(request.TaskMessage)
            };

            var responseMessage = _dataConverter.Deserialize<TaskMessage>(request.ResponseMessage);

            await _orchestrationService.CompleteTaskActivityWorkItemAsync(workItem, responseMessage);

            return new Empty();
        }

        public override async Task<Empty> AbandonTaskActivityWorkItem(AbandonTaskActivityWorkItemRequest request, ServerCallContext context)
        {
            var workItem = new TaskActivityWorkItem
            {
                Id = request.Id,
                LockedUntilUtc = request.LockedUntilUtc.ToDateTime(),
                TaskMessage = _dataConverter.Deserialize<TaskMessage>(request.TaskMessage)
            };

            await _orchestrationService.AbandonTaskActivityWorkItemAsync(workItem);

            return new Empty();
        }
    }
}
