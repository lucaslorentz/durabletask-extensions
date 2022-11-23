using System;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Common;
using DurableTask.Core.History;
using DurableTask.Core.Query;
using DurableTaskGrpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LLL.DurableTask.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DurableTaskGrpc.OrchestrationService;
using TaskActivityWorkItem = DurableTask.Core.TaskActivityWorkItem;
using TaskOrchestrationWorkItem = DurableTask.Core.TaskOrchestrationWorkItem;

namespace LLL.DurableTask.Server.Grpc.Server
{
    public class GrpcServerOrchestrationService : OrchestrationServiceBase
    {
        private readonly GrpcServerOrchestrationServiceOptions _options;
        private readonly IOrchestrationService _orchestrationService;
        private readonly IOrchestrationServiceClient _orchestrationServiceClient;
        private readonly ILogger<GrpcServerOrchestrationService> _logger;
        private readonly IDistributedOrchestrationService _distributedOrchestrationService;
        private readonly IOrchestrationServiceQueryClient _orchestrationServiceQueryClient;
        private readonly IOrchestrationServicePurgeClient _orchestrationServicePurgeClient;
        private readonly IOrchestrationServiceFeaturesClient _orchestrationServiceFeaturesClient;
        private readonly IOrchestrationServiceRewindClient _orchestrationServiceRewindClient;

        public GrpcServerOrchestrationService(
            IOptions<GrpcServerOrchestrationServiceOptions> options,
            IOrchestrationService orchestrationService,
            IOrchestrationServiceClient orchestrationServiceClient,
            ILogger<GrpcServerOrchestrationService> logger,
            IDistributedOrchestrationService distributedOrchestrationService = null,
            IOrchestrationServiceQueryClient orchestrationServiceQueryClient = null,
            IOrchestrationServicePurgeClient orchestrationServicePurgeClient = null,
            IOrchestrationServiceFeaturesClient orchestrationServiceFeaturesClient = null,
            IOrchestrationServiceRewindClient orchestrationServiceRewindClient = null)
        {
            _options = options.Value;
            _orchestrationService = orchestrationService;
            _orchestrationServiceClient = orchestrationServiceClient;
            _logger = logger;
            _distributedOrchestrationService = distributedOrchestrationService;
            _orchestrationServiceQueryClient = orchestrationServiceQueryClient;
            _orchestrationServicePurgeClient = orchestrationServicePurgeClient;
            _orchestrationServiceFeaturesClient = orchestrationServiceFeaturesClient;
            _orchestrationServiceRewindClient = orchestrationServiceRewindClient;
        }

        public override async Task<GetFeaturesResponse> GetFeatures(Empty request, ServerCallContext context)
        {
            var features = _orchestrationServiceFeaturesClient != null
                ? await _orchestrationServiceFeaturesClient.GetFeatures()
                : new OrchestrationFeature[0];

            return new GetFeaturesResponse
            {
                Features = { features.Select(f => (int)f) }
            };
        }

        public override async Task<Empty> CreateTaskOrchestration(CreateTaskOrchestrationRequest request, ServerCallContext context)
        {
            var creationMessage = _options.DataConverter.Deserialize<TaskMessage>(request.CreationMessage);
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

        public override async Task<Empty> RewindTaskOrchestration(RewindTaskOrchestrationRequest request, ServerCallContext context)
        {
            await (_orchestrationServiceRewindClient ?? throw NotSupported("Rewind"))
                .RewindTaskOrchestrationAsync(request.InstanceId, request.Reason);

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
                States = { states.Select(s => _options.DataConverter.Serialize(s)) }
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
                State = state == null ? null : _options.DataConverter.Serialize(state)
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
                State = state == null ? null : _options.DataConverter.Serialize(state)
            };

            return response;
        }

        public override async Task<GetOrchestrationWithQueryResponse> GetOrchestrationWithQuery(GetOrchestrationWithQueryRequest request, ServerCallContext context)
        {
            var query = new ExtendedOrchestrationQuery();
            query.RuntimeStatus = request.RuntimeStatus.Select(s => (OrchestrationStatus)s).ToArray();
            query.CreatedTimeFrom = request.CreatedTimeFrom?.ToDateTime();
            query.CreatedTimeTo = request.CreatedTimeTo?.ToDateTime();
            query.TaskHubNames = request.TaskHubNames;
            query.PageSize = request.PageSize;
            query.ContinuationToken = request.ContinuationToken;
            query.InstanceIdPrefix = request.InstanceIdPrefix;
            query.FetchInputsAndOutputs = request.FetchInputsAndOutputs;
            query.NamePrefix = request.NamePrefix;
            query.IncludePreviousExecutions = request.IncludePreviousExecutions;

            var queryResult = await _orchestrationServiceQueryClient.GetOrchestrationWithQueryAsync(query, context.CancellationToken);

            var response = new GetOrchestrationWithQueryResponse
            {
                OrchestrationState = { queryResult.OrchestrationState.Select(s => _options.DataConverter.Serialize(s)) },
                ContinuationToken = queryResult.ContinuationToken
            };

            return response;
        }

        public override async Task<PurgeInstanceHistoryResponse> PurgeInstanceHistory(PurgeInstanceHistoryRequest request, ServerCallContext context)
        {
            var client = _orchestrationServicePurgeClient ?? throw NotSupported("PurgeInstanceHistory");

            PurgeResult result;

            if (!string.IsNullOrEmpty(request.InstanceId))
            {
                result = await client.PurgeInstanceStateAsync(request.InstanceId);
            }
            else
            {
                var createdTimeFrom = request.CreatedTimeFrom?.ToDateTime() ?? DateTime.MinValue;
                var createdTimeTo = request.CreatedTimeTo?.ToDateTime();
                var runtimeStatus = request.RuntimeStatus.Select(s => (OrchestrationStatus)s).ToArray();

                var filter = new PurgeInstanceFilter(createdTimeFrom, createdTimeTo, runtimeStatus);

                result = await client.PurgeInstanceStateAsync(filter);
            }

            return new PurgeInstanceHistoryResponse
            {
                InstancesDeleted = result.DeletedInstanceCount
            };
        }

        public override async Task<Empty> SendTaskOrchestrationMessageBatch(SendTaskOrchestrationMessageBatchRequest request, ServerCallContext context)
        {
            var messages = request.Messages.Select(x => _options.DataConverter.Deserialize<TaskMessage>(x)).ToArray();

            await _orchestrationServiceClient.SendTaskOrchestrationMessageBatchAsync(messages);

            return new Empty();
        }

        public override async Task LockNextTaskOrchestrationWorkItem(IAsyncStreamReader<TaskOrchestrationRequest> requestStream, IServerStreamWriter<TaskOrchestrationResponse> responseStream, ServerCallContext context)
        {
            try
            {
                TaskOrchestrationWorkItem workItem = null;

                // Receive and reply each message
                await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    switch (message.MessageCase)
                    {
                        case TaskOrchestrationRequest.MessageOneofCase.LockRequest:
                            var lockRequest = message.LockRequest;
                            var orchestrations = lockRequest.Orchestrations.Select(x => new NameVersion(x.Name, x.Version)).ToArray();

                            workItem = await (lockRequest.AllOrchestrations
                                ? _orchestrationService
                                    .LockNextTaskOrchestrationWorkItemAsync(lockRequest.ReceiveTimeout.ToTimeSpan(), context.CancellationToken)
                                : (_distributedOrchestrationService ?? throw DistributedWorkersNotSupported())
                                    .LockNextTaskOrchestrationWorkItemAsync(lockRequest.ReceiveTimeout.ToTimeSpan(), orchestrations, context.CancellationToken)
                            );

                            var lockResponse = new TaskOrchestrationResponse
                            {
                                LockResponse = new LockNextTaskOrchestrationWorkItemResponse
                                {
                                    WorkItem = workItem == null ? null : new DurableTaskGrpc.TaskOrchestrationWorkItem
                                    {
                                        InstanceId = workItem.InstanceId,
                                        LockedUntilUtc = Timestamp.FromDateTime(workItem.LockedUntilUtc),
                                        Events = { workItem.OrchestrationRuntimeState.Events.Select(_options.DataConverter.Serialize) },
                                        NewMessages = { workItem.NewMessages.Select(_options.DataConverter.Serialize) }
                                    }
                                }
                            };

                            context.CancellationToken.ThrowIfCancellationRequested();

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

                            context.CancellationToken.ThrowIfCancellationRequested();

                            await responseStream.WriteAsync(renewResponse);
                            break;
                        case TaskOrchestrationRequest.MessageOneofCase.CompleteRequest:
                            var completeRequest = message.CompleteRequest;
                            var outboundMessages = completeRequest.OutboundMessages.Select(x => _options.DataConverter.Deserialize<TaskMessage>(x)).ToArray();
                            var timerMessages = completeRequest.TimerMessages.Select(x => _options.DataConverter.Deserialize<TaskMessage>(x)).ToArray();
                            var orchestratorMessages = completeRequest.OrchestratorMessages.Select(x => _options.DataConverter.Deserialize<TaskMessage>(x)).ToArray();
                            var continuedAsNewMessage = string.IsNullOrEmpty(completeRequest.ContinuedAsNewMessage)
                                ? null
                                : _options.DataConverter.Deserialize<TaskMessage>(completeRequest.ContinuedAsNewMessage);

                            var newEvents = completeRequest.NewEvents.Select(x => _options.DataConverter.Deserialize<HistoryEvent>(x)).ToArray();
                            workItem.OrchestrationRuntimeState ??= new OrchestrationRuntimeState();
                            foreach (var newEvent in newEvents)
                            {
                                workItem.OrchestrationRuntimeState.AddEvent(newEvent);
                            }
                            workItem.OrchestrationRuntimeState.Status = completeRequest.NewStatus;

                            var newOrchestrationRuntimeState = workItem.OrchestrationRuntimeState;
                            var newOrchestrationRuntimeStateEvents = completeRequest.NewOrchestrationEvents.Select(x => _options.DataConverter.Deserialize<HistoryEvent>(x)).ToArray();
                            if (newOrchestrationRuntimeStateEvents.Length > 0)
                            {
                                newOrchestrationRuntimeState = new OrchestrationRuntimeState();
                                foreach (var newEvent in newOrchestrationRuntimeStateEvents)
                                {
                                    newOrchestrationRuntimeState.AddEvent(newEvent);
                                }
                                newOrchestrationRuntimeState.Status = completeRequest.NewOrchestrationStatus;
                            }

                            var orchestrationState = Utils.BuildOrchestrationState(newOrchestrationRuntimeState);

                            await _orchestrationService.CompleteTaskOrchestrationWorkItemAsync(
                                workItem,
                                newOrchestrationRuntimeState,
                                outboundMessages,
                                orchestratorMessages,
                                timerMessages,
                                continuedAsNewMessage,
                                orchestrationState);

                            newOrchestrationRuntimeState.NewEvents.Clear();

                            workItem.OrchestrationRuntimeState = newOrchestrationRuntimeState;

                            context.CancellationToken.ThrowIfCancellationRequested();

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

                                context.CancellationToken.ThrowIfCancellationRequested();

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
                                            Messages = { newMessages.Select(_options.DataConverter.Serialize) }
                                        }
                                    }
                                };

                                context.CancellationToken.ThrowIfCancellationRequested();

                                await responseStream.WriteAsync(fetchResponse);
                            }
                            break;
                        case TaskOrchestrationRequest.MessageOneofCase.ReleaseRequest:
                            var releaseRequest = message.ReleaseRequest;
                            await _orchestrationService.ReleaseTaskOrchestrationWorkItemAsync(workItem);
                            context.CancellationToken.ThrowIfCancellationRequested();
                            await responseStream.WriteAsync(new TaskOrchestrationResponse
                            {
                                ReleaseResponse = new ReleaseTaskOrchestrationWorkItemResponse()
                            });
                            break;
                        case TaskOrchestrationRequest.MessageOneofCase.AbandonRequest:
                            var abandonRequest = message.AbandonRequest;
                            await _orchestrationService.AbandonTaskOrchestrationWorkItemAsync(workItem);
                            context.CancellationToken.ThrowIfCancellationRequested();
                            await responseStream.WriteAsync(new TaskOrchestrationResponse
                            {
                                AbandonResponse = new AbandonTaskOrchestrationWorkItemLockResponse()
                            });
                            break;
                    }
                }
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                // Avoid exceptions when clients cancel request
            }
        }

        public override async Task<LockNextTaskActivityWorkItemResponse> LockNextTaskActivityWorkItem(LockNextTaskActivityWorkItemRequest request, ServerCallContext context)
        {
            try
            {
                var activities = request.Activities.Select(x => new NameVersion(x.Name, x.Version)).ToArray();

                var workItem = await (request.AllActivities
                    ? _orchestrationService
                        .LockNextTaskActivityWorkItem(request.ReceiveTimeout.ToTimeSpan(), context.CancellationToken)
                    : (_distributedOrchestrationService ?? throw DistributedWorkersNotSupported())
                        .LockNextTaskActivityWorkItem(request.ReceiveTimeout.ToTimeSpan(), activities, context.CancellationToken));

                var response = new LockNextTaskActivityWorkItemResponse
                {
                    WorkItem = workItem == null ? null : ToGrpcWorkItem(workItem)
                };

                return response;
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                // Avoid exceptions when clients cancel request
                return null;
            }
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

            var responseMessage = _options.DataConverter.Deserialize<TaskMessage>(request.ResponseMessage);

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
                TaskMessage = _options.DataConverter.Serialize(workItem.TaskMessage)
            };
        }

        private TaskActivityWorkItem ToDurableTaskWorkItem(DurableTaskGrpc.TaskActivityWorkItem grpcWorkItem)
        {
            return new TaskActivityWorkItem
            {
                Id = grpcWorkItem.Id,
                LockedUntilUtc = grpcWorkItem.LockedUntilUtc.ToDateTime(),
                TaskMessage = _options.DataConverter.Deserialize<TaskMessage>(grpcWorkItem.TaskMessage)
            };
        }

        private Exception DistributedWorkersNotSupported()
        {
            return NotSupported("Distributed workers");
        }

        private Exception NotSupported(string operation)
        {
            return new NotSupportedException($"{operation} is not supported by storage implementation");
        }
    }
}
