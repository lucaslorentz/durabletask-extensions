using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Exceptions;
using DurableTask.Core.History;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Extensions;
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var events = await dbContext.Events
                    .Where(e => e.ExecutionId == executionId)
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                await RewindInstanceAsync(dbContext, instanceId, reason, historyEvents =>
                {
                    // For completed executions
                    var completedEvent = historyEvents.OfType<ExecutionCompletedEvent>().FirstOrDefault();
                    if (completedEvent != null)
                    {
                        if (completedEvent.OrchestrationStatus == OrchestrationStatus.Failed)
                        {
                            // For failed executions, rewind to last failed task or suborchestration
                            var lastFailedTask = historyEvents.LastOrDefault(h => h is TaskFailedEvent || h is SubOrchestrationInstanceFailedEvent);
                            if (lastFailedTask != null)
                                return lastFailedTask;
                        }

                        // Fallback to just reopenning orchestration, because error could have happened inside orchestrator function
                        return completedEvent;
                    }

                    // For terminated executions, only rewing the termination
                    var terminatedEvent = historyEvents.OfType<ExecutionTerminatedEvent>().FirstOrDefault();
                    if (terminatedEvent != null)
                        return terminatedEvent;

                    return null;
                });

                await dbContext.SaveChangesAsync();
            }
        }

        private async Task RewindInstanceAsync(OrchestrationDbContext dbContext, string instanceId, string reason, Func<IList<HistoryEvent>, HistoryEvent> getRewindPoint)
        {
            var instance = await _dbContextExtensions.LockInstanceForUpdate(dbContext, instanceId);

            var execution = await dbContext.Executions.FindAsync(instance.LastExecutionId);

            var eventsEntities = await dbContext.Events
                .Where(e => e.ExecutionId == execution.ExecutionId)
                .OrderBy(e => e.SequenceNumber)
                .ToArrayAsync();

            var historyEvents = eventsEntities.Select(e => _options.DataConverter.Deserialize<HistoryEvent>(e.Content))
                .ToArray();

            var rewindPoint = getRewindPoint(historyEvents);
            if (rewindPoint == null)
            {
                return;
            }

            var rewindResult = historyEvents.Rewind(rewindPoint, reason, _options.RewindDataConverter);

            foreach (var (eventEntity, eventHistory) in eventsEntities.Zip(rewindResult.HistoryEvents))
            {
                eventEntity.Content = _options.DataConverter.Serialize(eventHistory);
            }

            // Create child orchestrations
            foreach (var executionStartedEvent in rewindResult.OrchestratorMessages.Select(m => m.Event).OfType<ExecutionStartedEvent>())
            {
                var childInstance = _instanceMapper.CreateInstance(executionStartedEvent);
                await dbContext.Instances.AddAsync(childInstance);

                var childRuntimeState = new OrchestrationRuntimeState(new[] { executionStartedEvent });

                var childExecution = _executionMapper.CreateExecution(childRuntimeState);
                await dbContext.Executions.AddAsync(childExecution);
            }

            var orchestrationQueueName = QueueMapper.ToQueue(rewindResult.NewRuntimeState.Name, rewindResult.NewRuntimeState.Version);

            var knownQueues = new Dictionary<string, string>
            {
                [rewindResult.NewRuntimeState.OrchestrationInstance.InstanceId] = orchestrationQueueName
            };

            if (rewindResult.NewRuntimeState.ParentInstance != null)
                knownQueues[rewindResult.NewRuntimeState.ParentInstance.OrchestrationInstance.InstanceId] = QueueMapper.ToQueue(rewindResult.NewRuntimeState.ParentInstance.Name, rewindResult.NewRuntimeState.ParentInstance.Version);

            // Write messages
            var activityMessages = rewindResult.OutboundMessages
                .Select(m => _activityMessageMapper.CreateActivityMessage(m, orchestrationQueueName))
                .ToArray();

            await dbContext.ActivityMessages.AddRangeAsync(activityMessages);

            var allOrchestrationMessages = rewindResult.OrchestratorMessages
                .Concat(rewindResult.TimerMessages)
                .ToArray();

            await SendTaskOrchestrationMessagesAsync(dbContext, allOrchestrationMessages, knownQueues);

            // Update instance
            _instanceMapper.UpdateInstance(instance, rewindResult.NewRuntimeState);

            // Update current execution
            execution = await SaveExecutionAsync(dbContext, rewindResult.NewRuntimeState, execution);

            if (rewindResult.NewRuntimeState.ParentInstance != null)
            {
                await RewindInstanceAsync(dbContext, rewindResult.NewRuntimeState.ParentInstance.OrchestrationInstance.InstanceId, reason, historyEvents =>
                {
                    return historyEvents
                        .OfType<SubOrchestrationInstanceCompletedEvent>()
                        .FirstOrDefault(h => h.TaskScheduledId == rewindResult.NewRuntimeState.ParentInstance.TaskScheduleId);
                });
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
