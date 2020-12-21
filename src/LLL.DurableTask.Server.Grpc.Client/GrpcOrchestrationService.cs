using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using DurableTask.Core.Serializing;
using DurableTaskGrpc;
using Google.Protobuf.WellKnownTypes;
using LLL.DurableTask.Core;
using LLL.DurableTask.Server.Grpc.Client.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DurableTaskGrpc.OrchestrationService;
using TaskActivityWorkItem = DurableTask.Core.TaskActivityWorkItem;
using TaskOrchestrationWorkItem = DurableTask.Core.TaskOrchestrationWorkItem;

namespace LLL.DurableTask.Server.Client
{
    public partial class GrpcOrchestrationService :
        IOrchestrationService,
        IExtendedOrchestrationService
    {
        private const int DelayAfterFailureInSeconds = 5;

        private readonly DataConverter _dataConverter = new GrpcJsonDataConverter();

        private readonly OrchestrationServiceClient _client;
        private readonly ILogger _logger;
        private readonly GrpcOrchestrationServiceOptions _options;

        public int TaskOrchestrationDispatcherCount => 1;
        public int MaxConcurrentTaskOrchestrationWorkItems { get; } = 100;
        public int MaxConcurrentTaskActivityWorkItems { get; } = 20;
        public BehaviorOnContinueAsNew EventBehaviourForContinueAsNew => BehaviorOnContinueAsNew.Carryover;
        public int TaskActivityDispatcherCount => 1;

        public GrpcOrchestrationService(
            OrchestrationServiceClient client,
            IOptions<GrpcOrchestrationServiceOptions> options,
            ILogger<GrpcOrchestrationSession> logger)
        {
            _options = options.Value;
            _client = client;
            _logger = logger;
        }

        #region Setup
        public Task CreateAsync()
        {
            return CreateAsync(false);
        }

        public Task CreateAsync(bool recreateInstanceStore)
        {
            return Task.CompletedTask;
        }

        public Task CreateIfNotExistsAsync()
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync()
        {
            return DeleteAsync(false);
        }

        public Task DeleteAsync(bool deleteInstanceStore)
        {
            return Task.CompletedTask;
        }

        public int GetDelayInSecondsAfterOnFetchException(Exception exception)
        {
            return DelayAfterFailureInSeconds;
        }

        public int GetDelayInSecondsAfterOnProcessException(Exception exception)
        {
            return DelayAfterFailureInSeconds;
        }


        public bool IsMaxMessageCountExceeded(int currentMessageCount, OrchestrationRuntimeState runtimeState)
        {
            return false;
        }

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(bool isForced)
        {
            return Task.CompletedTask;
        }
        #endregion

        #region Orchestration
        public Task<TaskOrchestrationWorkItem> LockNextTaskOrchestrationWorkItemAsync(
            TimeSpan receiveTimeout, CancellationToken cancellationToken)
        {
            return LockNextTaskOrchestrationWorkItemAsync(
                receiveTimeout,
                new INameVersionInfo[0],
                true,
                cancellationToken
            );
        }

        public Task<TaskOrchestrationWorkItem> LockNextTaskOrchestrationWorkItemAsync(TimeSpan receiveTimeout, INameVersionInfo[] orchestrations, CancellationToken cancellationToken)
        {
            return LockNextTaskOrchestrationWorkItemAsync(
                receiveTimeout,
                orchestrations,
                false,
                cancellationToken
            );
        }

        private async Task<TaskOrchestrationWorkItem> LockNextTaskOrchestrationWorkItemAsync(
            TimeSpan receiveTimeout,
            INameVersionInfo[] orchestrations,
            bool allOrchesrations,
            CancellationToken cancellationToken)
        {
            var stream = _client.LockNextTaskOrchestrationWorkItem();

            try
            {
                var request = new TaskOrchestrationRequest
                {
                    LockRequest = new LockNextTaskOrchestrationWorkItemRequest
                    {
                        ReceiveTimeout = Duration.FromTimeSpan(receiveTimeout),
                        Orchestrations = {
                            orchestrations
                                .Select(nv => new NameVersion { Name = nv.Name, Version = nv.Version })
                        },
                        AllOrchestrations = allOrchesrations
                    }
                };

                await stream.RequestStream.WriteAsync(request);

                if (!await stream.ResponseStream.MoveNext(cancellationToken))
                    throw new Exception("Session aborted");

                if (stream.ResponseStream.Current.MessageCase != TaskOrchestrationResponse.MessageOneofCase.LockResponse)
                    throw new Exception("Didn't receive lock response");

                var lockResponse = stream.ResponseStream.Current.LockResponse;

                if (lockResponse.WorkItem == null)
                    return null;

                return new TaskOrchestrationWorkItem
                {
                    InstanceId = lockResponse.WorkItem.InstanceId,
                    OrchestrationRuntimeState = new OrchestrationRuntimeState(
                        lockResponse.WorkItem.Events
                            .Select(e => _dataConverter.Deserialize<HistoryEvent>(e))
                            .ToArray()),
                    LockedUntilUtc = lockResponse.WorkItem.LockedUntilUtc.ToDateTime(),
                    NewMessages = lockResponse.WorkItem.NewMessages.Select(m => _dataConverter.Deserialize<TaskMessage>(m)).ToArray(),
                    Session = new GrpcOrchestrationSession(stream, _logger)
                };
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public async Task RenewTaskOrchestrationWorkItemLockAsync(TaskOrchestrationWorkItem workItem)
        {
            await (workItem.Session as GrpcOrchestrationSession).Renew(workItem);
        }

        public async Task CompleteTaskOrchestrationWorkItemAsync(
            TaskOrchestrationWorkItem workItem,
            OrchestrationRuntimeState newOrchestrationRuntimeState,
            IList<TaskMessage> outboundMessages,
            IList<TaskMessage> orchestratorMessages,
            IList<TaskMessage> timerMessages,
            TaskMessage continuedAsNewMessage,
            OrchestrationState orchestrationState)
        {
            await (workItem.Session as GrpcOrchestrationSession).Complete(
                workItem,
                newOrchestrationRuntimeState,
                outboundMessages,
                orchestratorMessages,
                timerMessages,
                continuedAsNewMessage,
                orchestrationState);
        }

        public async Task ReleaseTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
        {
            await (workItem.Session as GrpcOrchestrationSession).Release(workItem);
        }

        public async Task AbandonTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
        {
            await (workItem.Session as GrpcOrchestrationSession).Abandon(workItem);
        }

        #endregion

        #region Activity
        public Task<TaskActivityWorkItem> LockNextTaskActivityWorkItem(TimeSpan receiveTimeout, CancellationToken cancellationToken)
        {
            return LockNextTaskActivityWorkItem(
                receiveTimeout,
                new INameVersionInfo[0],
                true,
                cancellationToken);
        }

        public Task<TaskActivityWorkItem> LockNextTaskActivityWorkItem(TimeSpan receiveTimeout, INameVersionInfo[] activities, CancellationToken cancellationToken)
        {
            return LockNextTaskActivityWorkItem(
                receiveTimeout,
                activities,
                false,
                cancellationToken);
        }

        private async Task<TaskActivityWorkItem> LockNextTaskActivityWorkItem(
            TimeSpan receiveTimeout,
            INameVersionInfo[] activities,
            bool allActivities,
            CancellationToken cancellationToken)
        {
            var request = new LockNextTaskActivityWorkItemRequest
            {
                ReceiveTimeout = Duration.FromTimeSpan(receiveTimeout),
                Activities = {
                    activities
                        .Select(nv => new NameVersion { Name = nv.Name, Version = nv.Version })
                },
                AllActivities = allActivities
            };

            var response = await _client.LockNextTaskActivityWorkItemAsync(request);

            if (response.WorkItem == null)
                return null;

            return ToDurableTaskWorkItem(response.WorkItem);
        }

        public async Task<TaskActivityWorkItem> RenewTaskActivityWorkItemLockAsync(TaskActivityWorkItem workItem)
        {
            var request = new RenewTaskActivityWorkItemLockRequest
            {
                WorkItem = ToGrpcWorkItem(workItem)
            };

            var response = await _client.RenewTaskActivityWorkItemLockAsync(request);

            return ToDurableTaskWorkItem(response.WorkItem);
        }

        public async Task CompleteTaskActivityWorkItemAsync(
            TaskActivityWorkItem workItem,
            TaskMessage responseMessage)
        {
            var request = new CompleteTaskActivityWorkItemRequest
            {
                WorkItem = ToGrpcWorkItem(workItem),
                ResponseMessage = _dataConverter.Serialize(responseMessage)
            };

            await _client.CompleteTaskActivityWorkItemAsync(request);
        }

        public async Task AbandonTaskActivityWorkItemAsync(TaskActivityWorkItem workItem)
        {
            var request = new AbandonTaskActivityWorkItemRequest
            {
                WorkItem = ToGrpcWorkItem(workItem)
            };

            await _client.AbandonTaskActivityWorkItemAsync(request);
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

        #endregion
    }
}
