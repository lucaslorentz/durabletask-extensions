using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Exceptions;
using DurableTask.Core.History;
using DurableTask.Core.Query;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Extensions;
using LLL.DurableTask.EFCore.Mappers;
using LLL.DurableTask.EFCore.Polling;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore
{
    public partial class EFCoreOrchestrationService :
        IOrchestrationServiceClient,
        IOrchestrationServiceFeaturesClient,
        IOrchestrationServiceQueryClient,
        IOrchestrationServicePurgeClient,
        IOrchestrationServiceRewindClient
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

        public async Task<PurgeResult> PurgeInstanceStateAsync(string instanceId)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var deletedRows = await _dbContextExtensions.PurgeInstanceHistoryAsync(dbContext, instanceId);

                return new PurgeResult(deletedRows);
            }
        }

        public async Task<PurgeResult> PurgeInstanceStateAsync(PurgeInstanceFilter purgeInstanceFilter)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var deletedRows = await _dbContextExtensions.PurgeInstanceHistoryAsync(dbContext, purgeInstanceFilter);

                return new PurgeResult(deletedRows);
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
                OrchestrationFeature.Rewind,
                OrchestrationFeature.Tags,
                OrchestrationFeature.StatePerExecution
            });
        }

        public async Task<OrchestrationQueryResult> GetOrchestrationWithQueryAsync(OrchestrationQuery query, CancellationToken cancellationToken)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var extendedQuery = query as ExtendedOrchestrationQuery;

                var queryable = extendedQuery == null || extendedQuery.IncludePreviousExecutions
                    ? dbContext.Executions
                    : _dbContextExtensions.LatestExecutions(dbContext);

                if (!string.IsNullOrEmpty(query.InstanceIdPrefix))
                    queryable = queryable.Where(e => e.InstanceId.StartsWith(query.InstanceIdPrefix));

                if (query.CreatedTimeFrom != null)
                    queryable = queryable.Where(e => e.CreatedTime >= query.CreatedTimeFrom);

                if (query.CreatedTimeTo != null)
                    queryable = queryable.Where(e => e.CreatedTime <= query.CreatedTimeTo);

                if (query.RuntimeStatus != null && query.RuntimeStatus.Any())
                    queryable = queryable.Where(e => query.RuntimeStatus.Contains(e.Status));

                if (extendedQuery != null)
                {
                    if (!string.IsNullOrEmpty(extendedQuery.NamePrefix))
                        queryable = queryable.Where(e => e.Name.StartsWith(extendedQuery.NamePrefix));

                    foreach (var kv in extendedQuery.Tags)
                        queryable = queryable.Where(e => e.Tags.Any(t => t.Name == kv.Key && t.Value == kv.Value));
                }

                var continuationToken = EFCoreContinuationToken.Parse(query.ContinuationToken);
                if (continuationToken != null)
                {
                    queryable = queryable.Where(i =>
                        i.CreatedTime < continuationToken.CreatedTime ||
                        i.CreatedTime == continuationToken.CreatedTime && continuationToken.InstanceId.CompareTo(i.InstanceId) < 0);
                }

                var instances = await queryable
                    .OrderByDescending(x => x.CreatedTime)
                    .ThenByDescending(x => x.InstanceId)
                    .Take(query.PageSize + 1)
                    .ToArrayAsync();

                var pageInstances = instances
                    .Take(query.PageSize)
                    .ToArray();

                var mappedPageInstances = pageInstances
                    .Select(_executionMapper.MapToState)
                    .ToArray();

                var newContinuationToken = instances.Length > pageInstances.Length
                    ? new EFCoreContinuationToken
                    {
                        CreatedTime = pageInstances.Last().CreatedTime,
                        InstanceId = pageInstances.Last().InstanceId,
                    }.Serialize()
                    : null;

                return new OrchestrationQueryResult(mappedPageInstances, newContinuationToken);
            }
        }

        public async Task RewindTaskOrchestrationAsync(string instanceId, string reason)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                await RewindInstanceAsync(dbContext, instanceId, reason, true, FindLastErrorOrCompletionRewindPoint);
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task RewindInstanceAsync(OrchestrationDbContext dbContext, string instanceId, string reason, bool rewindParents, Func<IList<HistoryEvent>, HistoryEvent> findRewindPoint)
        {
            var instance = await _dbContextExtensions.LockInstanceForUpdate(dbContext, instanceId);

            var execution = await dbContext.Executions
                .Where(e => e.ExecutionId == instance.LastExecutionId)
                .Include(e => e.Events)
                .SingleAsync();

            var deserializedEvents = execution.Events
                .OrderBy(e => e.SequenceNumber)
                .Select(e => _options.DataConverter.Deserialize<HistoryEvent>(e.Content))
                .ToArray();

            var rewindPoint = findRewindPoint(deserializedEvents);
            if (rewindPoint == null)
            {
                return;
            }

            var rewindResult = deserializedEvents.Rewind(rewindPoint, reason, _options.DataConverter);

            // Rewind suborchestrations
            foreach (var instanceIdToRewind in rewindResult.SubOrchestrationsInstancesToRewind)
            {
                await RewindInstanceAsync(dbContext, instanceIdToRewind, reason, false, FindLastErrorOrCompletionRewindPoint);
            }

            var orchestrationQueueName = QueueMapper.ToQueue(rewindResult.NewRuntimeState.Name, rewindResult.NewRuntimeState.Version);

            // Write activity messages
            var activityMessages = rewindResult.OutboundMessages
                .Select(m => _activityMessageMapper.CreateActivityMessage(m, orchestrationQueueName))
                .ToArray();

            await dbContext.ActivityMessages.AddRangeAsync(activityMessages);

            // Write orchestration messages
            var knownQueues = new Dictionary<string, string>
            {
                [rewindResult.NewRuntimeState.OrchestrationInstance.InstanceId] = orchestrationQueueName
            };

            if (rewindResult.NewRuntimeState.ParentInstance != null)
                knownQueues[rewindResult.NewRuntimeState.ParentInstance.OrchestrationInstance.InstanceId] = QueueMapper.ToQueue(rewindResult.NewRuntimeState.ParentInstance.Name, rewindResult.NewRuntimeState.ParentInstance.Version);

            var allOrchestrationMessages = rewindResult.OrchestratorMessages
                .Concat(rewindResult.TimerMessages)
                .ToArray();

            await SendTaskOrchestrationMessagesAsync(dbContext, allOrchestrationMessages, knownQueues);

            // Update instance
            _instanceMapper.UpdateInstance(instance, rewindResult.NewRuntimeState);

            // Update current execution
            execution = await SaveExecutionAsync(dbContext, rewindResult.NewRuntimeState, execution);

            // Rewind parents
            if (rewindParents && rewindResult.NewRuntimeState.ParentInstance != null)
            {
                await RewindInstanceAsync(dbContext, rewindResult.NewRuntimeState.ParentInstance.OrchestrationInstance.InstanceId, reason, true, historyEvents =>
                {
                    return historyEvents
                        .OfType<SubOrchestrationInstanceCompletedEvent>()
                        .FirstOrDefault(h => h.TaskScheduledId == rewindResult.NewRuntimeState.ParentInstance.TaskScheduleId);
                });
            }
        }

        private HistoryEvent FindLastErrorOrCompletionRewindPoint(IList<HistoryEvent> historyEvents)
        {
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
