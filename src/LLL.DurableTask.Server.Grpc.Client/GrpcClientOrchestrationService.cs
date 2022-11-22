using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using DurableTaskGrpc;
using Google.Protobuf.WellKnownTypes;
using LLL.DurableTask.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DurableTaskGrpc.OrchestrationService;
using TaskActivityWorkItem = DurableTask.Core.TaskActivityWorkItem;
using TaskOrchestrationWorkItem = DurableTask.Core.TaskOrchestrationWorkItem;

namespace LLL.DurableTask.Server.Client
{
    public partial class GrpcClientOrchestrationService :
        IOrchestrationService,
        IDistributedOrchestrationService
    {
        private readonly GrpcClientOrchestrationServiceOptions _options;
        private readonly OrchestrationServiceClient _client;
        private readonly ILogger _logger;

        public int TaskOrchestrationDispatcherCount => _options.TaskOrchestrationDispatcherCount;
        public int MaxConcurrentTaskOrchestrationWorkItems => _options.MaxConcurrentTaskOrchestrationWorkItems;
        public int MaxConcurrentTaskActivityWorkItems => _options.MaxConcurrentTaskActivityWorkItems;
        public BehaviorOnContinueAsNew EventBehaviourForContinueAsNew => BehaviorOnContinueAsNew.Carryover;
        public int TaskActivityDispatcherCount => _options.TaskActivityDispatcherCount;

        public GrpcClientOrchestrationService(
            IOptions<GrpcClientOrchestrationServiceOptions> options,
            OrchestrationServiceClient client,
            ILogger<GrpcClientOrchestrationSession> logger)
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
            return _options.DelayInSecondsAfterFailure;
        }

        public int GetDelayInSecondsAfterOnProcessException(Exception exception)
        {
            return _options.DelayInSecondsAfterFailure;
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
            var stream = _client.LockNextTaskOrchestrationWorkItem(cancellationToken: cancellationToken);

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
                            .Select(e => _options.DataConverter.Deserialize<HistoryEvent>(e))
                            .ToArray()),
                    LockedUntilUtc = lockResponse.WorkItem.LockedUntilUtc.ToDateTime(),
                    NewMessages = lockResponse.WorkItem.NewMessages.Select(m => _options.DataConverter.Deserialize<TaskMessage>(m)).ToArray(),
                    Session = new GrpcClientOrchestrationSession(_options, stream, _logger)
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
            await (workItem.Session as GrpcClientOrchestrationSession).Renew(workItem);
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
            await (workItem.Session as GrpcClientOrchestrationSession).Complete(
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
            await (workItem.Session as GrpcClientOrchestrationSession).Release(workItem);
        }

        public async Task AbandonTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
        {
            await (workItem.Session as GrpcClientOrchestrationSession).Abandon(workItem);
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

            var response = await _client.LockNextTaskActivityWorkItemAsync(request, cancellationToken: cancellationToken);

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
                ResponseMessage = _options.DataConverter.Serialize(responseMessage)
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
                LockedUntilUtc = ToTimestamp(workItem.LockedUntilUtc),
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

        #endregion

        private Timestamp ToTimestamp(DateTime? dateTime)
        {
            if (dateTime == null)
                return null;

            return ToTimestamp(dateTime.Value);
        }

        private Timestamp ToTimestamp(DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Local:
                    dateTime = dateTime.ToUniversalTime();
                    break;
                case DateTimeKind.Unspecified:
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                    break;
            }

            return dateTime.ToTimestamp();
        }
    }
}
