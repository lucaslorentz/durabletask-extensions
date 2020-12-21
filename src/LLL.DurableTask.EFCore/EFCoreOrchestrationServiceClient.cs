using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Polling;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore
{
    public abstract partial class EFCoreOrchestrationService :
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

                var instance = await dbContext.Instances.FindAsync(instanceId);

                if (instance != null && dedupeStatuses != null && dedupeStatuses.Contains(instance.LastExecution.Status))
                {
                    throw new Exception($"Orchestration with id {instanceId} already exists");
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

                var orchestrationWorkItem = _orchestratorMessageMapper.CreateOrchestratorMessage(creationMessage, 0);
                await dbContext.OrchestratorMessages.AddAsync(orchestrationWorkItem);

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
                await PurgeOrchestrationHistoryAsync(dbContext, thresholdDateTimeUtc, timeRangeFilterType);
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
                var orchestrationMessage = messages
                    .Select(_orchestratorMessageMapper.CreateOrchestratorMessage)
                    .ToArray();

                await dbContext.OrchestratorMessages.AddRangeAsync(orchestrationMessage);

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
            s => IsFinalStatus(s.OrchestrationStatus),
            timeout,
            _options.PollingInterval,
            stoppableCancellationToken);

            if (!IsFinalStatus(state.OrchestrationStatus))
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
                OrchestrationFeature.QueryCount
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
                var deletedRows = await PurgeInstanceHistoryAsync(dbContext, instanceId);

                return new PurgeInstanceHistoryResult
                {
                    InstancesDeleted = deletedRows > 0 ? 1 : 0
                };
            }
        }

        protected abstract Task PurgeOrchestrationHistoryAsync(OrchestrationDbContext dbContext, DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType);

        protected abstract Task<int> PurgeInstanceHistoryAsync(OrchestrationDbContext dbContext, string instanceId);

        private bool IsFinalStatus(OrchestrationStatus status)
        {
            return status != OrchestrationStatus.Running &&
                status != OrchestrationStatus.Pending &&
                status != OrchestrationStatus.ContinuedAsNew;
        }
    }
}
