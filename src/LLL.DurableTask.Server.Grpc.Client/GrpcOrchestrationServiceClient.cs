using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using DurableTaskHub;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LLL.DurableTask.Core;
using LLL.DurableTask.Server.Grpc.Client.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DurableTaskHub.OrchestrationService;

namespace LLL.DurableTask.Server.Client
{
    public class GrpcOrchestrationServiceClient :
        IOrchestrationServiceClient,
        IExtendedOrchestrationServiceClient
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

        public IList<OrchestrationFeature> Features => throw new NotImplementedException();

        public GrpcOrchestrationServiceClient(
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

        public async Task ForceTerminateTaskOrchestrationAsync(string instanceId, string reason)
        {
            var request = new ForceTerminateTaskOrchestrationRequest
            {
                InstanceId = instanceId,
                Reason = reason
            };

            await _client.ForceTerminateTaskOrchestrationAsync(request);
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

        #region Client
        public async Task CreateTaskOrchestrationAsync(TaskMessage creationMessage)
        {
            await CreateTaskOrchestrationAsync(creationMessage, new OrchestrationStatus[0]);
        }

        public async Task CreateTaskOrchestrationAsync(TaskMessage creationMessage, OrchestrationStatus[] dedupeStatuses)
        {
            var request = new CreateTaskOrchestrationRequest
            {
                CreationMessage = _dataConverter.Serialize(creationMessage)
            };

            if (dedupeStatuses != null)
                request.DedupeStatuses.AddRange(dedupeStatuses.Select(s => (int)s));

            await _client.CreateTaskOrchestrationAsync(request);
        }

        public async Task<string> GetOrchestrationHistoryAsync(string instanceId, string executionId)
        {
            var request = new GetOrchestrationHistoryRequest
            {
                InstanceId = instanceId,
                ExecutionId = executionId
            };

            var response = await _client.GetOrchestrationHistoryAsync(request);

            return response.History;
        }

        public async Task<IList<OrchestrationState>> GetOrchestrationStateAsync(string instanceId, bool allExecutions)
        {
            var request = new GetOrchestrationInstanceStateRequest
            {
                InstanceId = instanceId,
                AllExecutions = allExecutions
            };

            var response = await _client.GetOrchestrationInstanceStateAsync(request);

            return response.States
                .Select(s => _dataConverter.Deserialize<OrchestrationState>(s))
                .ToArray();
        }

        public async Task<OrchestrationState> GetOrchestrationStateAsync(string instanceId, string executionId)
        {
            var request = new GetOrchestrationStateRequest
            {
                InstanceId = instanceId,
                ExecutionId = executionId
            };

            var response = await _client.GetOrchestrationStateAsync(request);

            return _dataConverter.Deserialize<OrchestrationState>(response.State);
        }

        public async Task PurgeOrchestrationHistoryAsync(DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            var request = new PurgeOrchestrationHistoryRequest
            {
                ThresholdDateTimeUtc = Timestamp.FromDateTime(thresholdDateTimeUtc),
                TimeRangeFilterType = (OrchestrationTimeFilterType)timeRangeFilterType
            };

            await _client.PurgeOrchestrationHistoryAsync(request);
        }

        public async Task SendTaskOrchestrationMessageAsync(TaskMessage message)
        {
            await SendTaskOrchestrationMessageBatchAsync(message);
        }

        public async Task SendTaskOrchestrationMessageBatchAsync(params TaskMessage[] messages)
        {
            var request = new SendTaskOrchestrationMessageBatchRequest
            {
                Messages = { messages.Select(_dataConverter.Serialize) }
            };

            await _client.SendTaskOrchestrationMessageBatchAsync(request);
        }

        public async Task<OrchestrationState> WaitForOrchestrationAsync(string instanceId, string executionId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var request = new WaitForOrchestrationRequest
            {
                InstanceId = instanceId,
                ExecutionId = executionId,
                Timeout = Duration.FromTimeSpan(timeout)
            };

            var callOptions = new CallOptions(cancellationToken: cancellationToken);

            var response = await _client.WaitForOrchestrationAsync(request, callOptions);

            return _dataConverter.Deserialize<OrchestrationState>(response.State);
        }

        public async Task<OrchestrationQueryResult> GetOrchestrationsAsync(OrchestrationQuery query, CancellationToken cancellationToken = default)
        {
            var request = new GetOrchestrationsRequest
            {
                Top = query.Top,
                ContinuationToken = query.ContinuationToken ?? string.Empty,
                InstanceId = query.InstanceId ?? string.Empty,
                Name = query.Name ?? string.Empty,
                CreatedTimeFrom = query.CreatedTimeFrom?.ToTimestamp(),
                CreatedTimeTo = query.CreatedTimeTo?.ToTimestamp()
            };

            if (query.RuntimeStatus != null)
                request.RuntimeStatus.AddRange(query.RuntimeStatus.Select(s => (int)s));

            var callOptions = new CallOptions(cancellationToken: cancellationToken);

            var response = await _client.GetOrchestrationsAsync(request, callOptions);

            return new OrchestrationQueryResult
            {
                Orchestrations = response.States
                    .Select(s => _dataConverter.Deserialize<OrchestrationState>(s))
                    .ToArray(),
                Count = response.CountIsNull ? default(long?) : response.Count,
                ContinuationToken = response.ContinuationToken
            };
        }

        public async Task<PurgeInstanceHistoryResult> PurgeInstanceHistoryAsync(string instanceId)
        {
            var request = new PurgeInstanceHistoryRequest
            {
                InstanceId = instanceId
            };

            var result = await _client.PurgeInstanceHistoryAsync(request);

            return new PurgeInstanceHistoryResult
            {
                InstancesDeleted = result.InstancesDeleted
            };
        }
        #endregion
    }
}
