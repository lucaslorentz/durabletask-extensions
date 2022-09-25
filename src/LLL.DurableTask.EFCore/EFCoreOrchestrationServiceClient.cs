using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Exceptions;
using DurableTask.Core.History;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Mappers;
using LLL.DurableTask.EFCore.Polling;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore
{
    public partial class EFCoreOrchestrationService :
        IOrchestrationServiceClient,
        IExtendedOrchestrationServiceClient
    {
        public Task CreateTaskOrchestrationAsync(TaskMessage creationMessage)
        {
            return CreateTaskOrchestrationAsync(creationMessage, null);
        }

        public async Task CreateTaskOrchestrationAsync(TaskMessage creationMessage, OrchestrationStatus[] dedupeStatuses)
        {
            using (var dbContext = _dbContextFactory())
            {
                var executionStartedEvent = creationMessage.Event as ExecutionStartedEvent;

                var instanceId = creationMessage.OrchestrationInstance.InstanceId;
                var executionId = creationMessage.OrchestrationInstance.ExecutionId;

                var instance = await _dbContextExtensions.LockInstanceForUpdate(dbContext, instanceId);

                if (instance != null)
                {
                    var lastExecution = await dbContext.Executions.FindAsync(instance.LastExecutionId);

                    // Dedupe dedupeStatuses silently
                    if (dedupeStatuses != null && dedupeStatuses.Contains(lastExecution.Status))
                        return;

                    // Otherwise, dedupe to avoid multile runnning instances
                    if (!IsFinalInstanceStatus(lastExecution.Status))
                        throw new OrchestrationAlreadyExistsException("Orchestration already has a running execution");
                }

                var runtimeState = new OrchestrationRuntimeState(new[] { executionStartedEvent });

                if (instance == null)
                {
                    instance = _instanceMapper.CreateInstance(executionStartedEvent);
                    await dbContext.Instances.AddAsync(instance);
                }
                else
                {
                    _instanceMapper.UpdateInstance(instance, runtimeState);
                }

                var execution = _executionMapper.CreateExecution(runtimeState);
                await dbContext.Executions.AddAsync(execution);

                var knownQueues = new Dictionary<string, string>
                {
                    [instance.InstanceId] = QueueMapper.ToQueue(runtimeState.Name, runtimeState.Version)
                };

                await SendTaskOrchestrationMessagesAsync(dbContext, new[] { creationMessage }, knownQueues);

                await dbContext.SaveChangesAsync();
            }
        }

        public Task ForceTerminateTaskOrchestrationAsync(string instanceId, string reason)
        {
            var taskMessage = new TaskMessage
            {
                OrchestrationInstance = new OrchestrationInstance { InstanceId = instanceId },
                Event = new ExecutionTerminatedEvent(-1, reason)
            };

            return SendTaskOrchestrationMessageAsync(taskMessage);
        }

        public async Task<string> GetOrchestrationHistoryAsync(string instanceId, string executionId)
        {
            using (var dbContext = _dbContextFactory())
            {
                var events = await dbContext.Events
                    .Where(e => e.InstanceId == instanceId && e.ExecutionId == executionId)
                    .OrderBy(e => e.SequenceNumber)
                    .ToArrayAsync();

                return $"[{string.Join(",", events.Select(e => e.Content))}]";
            }
        }

        public async Task<IList<OrchestrationState>> GetOrchestrationStateAsync(string instanceId, bool allExecutions)
        {
            var state = await GetOrchestrationStateAsync(instanceId, null);
            if (state == null)
                return new OrchestrationState[0];

            return new[] { state };
        }

        public async Task<OrchestrationState> GetOrchestrationStateAsync(string instanceId, string executionId)
        {
            using (var dbContext = _dbContextFactory())
            {
                var instance = await dbContext.Executions
                    .Where(e => e.InstanceId == instanceId && (executionId == null || e.ExecutionId == executionId))
                    .OrderByDescending(e => e.CreatedTime)
                    .FirstOrDefaultAsync();

                if (instance == null)
                    return null;

                return _executionMapper.MapToState(instance);
            }
        }

        public async Task PurgeOrchestrationHistoryAsync(DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            using (var dbContext = _dbContextFactory())
            {
                await _dbContextExtensions.PurgeOrchestrationHistoryAsync(dbContext, thresholdDateTimeUtc, timeRangeFilterType);
            }
        }

        public Task SendTaskOrchestrationMessageAsync(TaskMessage message)
        {
            return SendTaskOrchestrationMessageBatchAsync(message);
        }

        public async Task SendTaskOrchestrationMessageBatchAsync(params TaskMessage[] messages)
        {
            using (var dbContext = _dbContextFactory())
            {
                await SendTaskOrchestrationMessagesAsync(dbContext, messages);

                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<OrchestrationState> WaitForOrchestrationAsync(
            string instanceId,
            string executionId,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException(nameof(instanceId));
            }

            var stoppableCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _stopCts.Token).Token;

            var state = await BackoffPollingHelper.PollAsync(async () =>
            {
                return await GetOrchestrationStateAsync(instanceId, executionId);
            },
            s => IsFinalExecutionStatus(s.OrchestrationStatus),
            timeout,
            _options.PollingInterval,
            stoppableCancellationToken);

            if (!IsFinalExecutionStatus(state.OrchestrationStatus))
                return null;

            return state;
        }

        public Task<OrchestrationFeature[]> GetFeatures()
        {
            return Task.FromResult(new OrchestrationFeature[]
            {
                OrchestrationFeature.SearchByInstanceId,
                OrchestrationFeature.SearchByName,
                OrchestrationFeature.SearchByCreatedTime,
                OrchestrationFeature.SearchByStatus,
                OrchestrationFeature.QueryCount,
                OrchestrationFeature.Rewind,
                OrchestrationFeature.Tags,
                OrchestrationFeature.StatePerExecution
            });
        }

        public async Task<OrchestrationQueryResult> GetOrchestrationsAsync(OrchestrationQuery query, CancellationToken cancellationToken = default)
        {
            using (var dbContext = _dbContextFactory())
            {
                var queryable = dbContext.Executions as IQueryable<Entities.Execution>;

                if (!string.IsNullOrEmpty(query.InstanceId))
                    queryable = queryable.Where(i => i.InstanceId.StartsWith(query.InstanceId));

                if (!string.IsNullOrEmpty(query.Name))
                    queryable = queryable.Where(i => i.Name.StartsWith(query.Name));

                if (query.CreatedTimeFrom != null)
                    queryable = queryable.Where(i => i.CreatedTime >= query.CreatedTimeFrom);

                if (query.CreatedTimeTo != null)
                    queryable = queryable.Where(i => i.CreatedTime <= query.CreatedTimeTo);

                if (query.RuntimeStatus != null && query.RuntimeStatus.Any())
                    queryable = queryable.Where(i => query.RuntimeStatus.Contains(i.Status));

                long index;
                long count;

                var continuationToken = EFCoreContinuationToken.Parse(query.ContinuationToken);
                if (continuationToken != null)
                {
                    index = continuationToken.Index;
                    count = continuationToken.Count;

                    queryable = queryable.Where(i =>
                        i.CreatedTime < continuationToken.CreatedTime ||
                        i.CreatedTime == continuationToken.CreatedTime && continuationToken.InstanceId.CompareTo(i.InstanceId) < 0);
                }
                else
                {
                    index = 0;
                    count = await queryable.LongCountAsync();
                }

                var instances = await queryable
                    .OrderByDescending(x => x.CreatedTime)
                    .ThenByDescending(x => x.InstanceId)
                    .Take(query.Top)
                    .ToArrayAsync();

                var mappedInstances = instances
                    .Select(_executionMapper.MapToState)
                    .ToArray();

                return new OrchestrationQueryResult
                {
                    Orchestrations = mappedInstances,
                    Count = count,
                    ContinuationToken = count > index + instances.Length && instances.Length > 0
                        ? new EFCoreContinuationToken
                        {
                            Index = index + instances.Length,
                            Count = count,
                            CreatedTime = instances.Last().CreatedTime,
                            InstanceId = instances.Last().InstanceId,
                        }.Serialize()
                        : null
                };
            }
        }

        public async Task<PurgeInstanceHistoryResult> PurgeInstanceHistoryAsync(string instanceId)
        {
            using (var dbContext = _dbContextFactory())
            {
                var deletedRows = await _dbContextExtensions.PurgeInstanceHistoryAsync(dbContext, instanceId);

                return new PurgeInstanceHistoryResult
                {
                    InstancesDeleted = deletedRows > 0 ? 1 : 0
                };
            }
        }

        public async Task RewindTaskOrchestrationAsync(string instanceId, string reason)
        {
            using (var dbContext = _dbContextFactory())
            {
                await RewindInstanceAsync(dbContext, instanceId, reason);

                await dbContext.SaveChangesAsync();
            }
        }

        private async Task RewindInstanceAsync(OrchestrationDbContext dbContext, string instanceId, string reason)
        {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // REWIND ALGORITHM:
            // 1. Finds failed execution of specified orchestration instance to rewind
            // 2. Finds failure entities to clear and over-writes them (as well as corresponding trigger events)
            // 3. Identifies sub-orchestration failure(s) from parent instance and calls RewindHistoryAsync recursively on failed sub-orchestration child instance(s)
            // 4. Resets orchestration status of rewound instance in instance store table to prepare it to be restarted
            // 5. Restart that doesn't have failed suborchestrations with a generic event
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var lastExecution = dbContext.Instances
                                .Where(i => i.InstanceId == instanceId)
                                .Select(i => i.LastExecution)
                                .FirstOrDefault();

            var events = await dbContext.Events
                .Where(e => e.ExecutionId == lastExecution.ExecutionId)
                .ToArrayAsync();

            var historyEvents = events
                .ToDictionary(e => _options.DataConverter.Deserialize<HistoryEvent>(e.Content));

            bool hasFailedSubOrchestrations = false;

            foreach (var historyEvent in historyEvents.Keys)
            {
                if (historyEvent is TaskFailedEvent taskFailedEvent)
                {
                    var taskScheduledEvent = historyEvents.Keys.OfType<TaskScheduledEvent>()
                        .FirstOrDefault(e => e.EventId == taskFailedEvent.TaskScheduledId);

                    var rewoundTaskScheduledData = _options.RewindDataConverter.Serialize(new
                    {
                        taskScheduledEvent.EventType,
                        taskScheduledEvent.Name,
                        taskScheduledEvent.Version,
                        taskScheduledEvent.Input
                    });

                    historyEvents[taskScheduledEvent].Content = _options.DataConverter.Serialize(
                        new GenericEvent(taskScheduledEvent.EventId, $"Rewound: {rewoundTaskScheduledData}")
                        {
                            Timestamp = taskScheduledEvent.Timestamp
                        }
                    );

                    var rewoundTaskFailedData = _options.RewindDataConverter.Serialize(new
                    {
                        taskFailedEvent.EventType,
                        taskFailedEvent.Reason,
                        taskFailedEvent.TaskScheduledId
                    });

                    historyEvents[taskFailedEvent].Content = _options.DataConverter.Serialize(
                        new GenericEvent(taskFailedEvent.EventId, $"Rewound: {rewoundTaskFailedData}")
                        {
                            Timestamp = taskFailedEvent.Timestamp
                        }
                    );
                }
                else if (historyEvent is SubOrchestrationInstanceFailedEvent soFailedEvent)
                {
                    hasFailedSubOrchestrations = true;

                    var soCreatedEvent = historyEvents.Keys.OfType<SubOrchestrationInstanceCreatedEvent>()
                        .FirstOrDefault(e => e.EventId == soFailedEvent.TaskScheduledId);

                    var rewoundSoCreatedData = _options.RewindDataConverter.Serialize(new
                    {
                        soCreatedEvent.EventType,
                        soCreatedEvent.Name,
                        soCreatedEvent.Version,
                        soCreatedEvent.Input
                    });

                    historyEvents[soCreatedEvent].Content = _options.DataConverter.Serialize(
                        new GenericEvent(soCreatedEvent.EventId, $"Rewound: {rewoundSoCreatedData}")
                        {
                            Timestamp = soCreatedEvent.Timestamp
                        }
                    );

                    var rewoundSoFailedData = _options.RewindDataConverter.Serialize(new
                    {
                        soFailedEvent.EventType,
                        soFailedEvent.Reason,
                        soFailedEvent.TaskScheduledId
                    });

                    historyEvents[soFailedEvent].Content = _options.DataConverter.Serialize(
                        new GenericEvent(soFailedEvent.EventId, $"Rewound: {rewoundSoFailedData}")
                        {
                            Timestamp = soFailedEvent.Timestamp
                        }
                    );

                    // recursive call to clear out failure events on child instances
                    await RewindInstanceAsync(dbContext, soCreatedEvent.InstanceId, reason);
                }
                else if (historyEvent is ExecutionCompletedEvent executionCompletedEvent
                    && executionCompletedEvent.OrchestrationStatus == OrchestrationStatus.Failed)
                {
                    var rewoundExecutionCompletedData = _options.RewindDataConverter.Serialize(new
                    {
                        executionCompletedEvent.EventType,
                        executionCompletedEvent.Result,
                        executionCompletedEvent.OrchestrationStatus
                    });

                    historyEvents[executionCompletedEvent].Content = _options.DataConverter.Serialize(
                        new GenericEvent(executionCompletedEvent.EventId, $"Rewound: {rewoundExecutionCompletedData}")
                        {
                            Timestamp = executionCompletedEvent.Timestamp
                        }
                    );
                }
            }

            // Reset execution status
            lastExecution.Status = OrchestrationStatus.Running;
            lastExecution.LastUpdatedTime = DateTime.UtcNow;

            if (!hasFailedSubOrchestrations)
            {
                var orchestrationInstance = new OrchestrationInstance
                {
                    InstanceId = instanceId
                };

                var taskMessage = new TaskMessage
                {
                    OrchestrationInstance = orchestrationInstance,
                    Event = new GenericEvent(-1, reason)
                };

                var knownQueues = new Dictionary<string, string>
                {
                    [lastExecution.InstanceId] = QueueMapper.ToQueue(lastExecution.Name, lastExecution.Version)
                };

                await SendTaskOrchestrationMessagesAsync(dbContext, new[] { taskMessage }, knownQueues);
            }
        }

        private bool IsFinalInstanceStatus(OrchestrationStatus status)
        {
            return IsFinalExecutionStatus(status) &&
                status != OrchestrationStatus.ContinuedAsNew;
        }

        private bool IsFinalExecutionStatus(OrchestrationStatus status)
        {
            return status != OrchestrationStatus.Running &&
                status != OrchestrationStatus.Pending;
        }
    }
}
