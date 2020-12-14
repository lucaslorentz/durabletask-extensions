using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace LLL.DurableTask.EFCore
{
    public abstract class EFCoreOrchestrationServiceClient :
        IOrchestrationServiceClient,
        IExtendedOrchestrationServiceClient
    {
        private readonly static TimeSpan _statusPollingInterval = TimeSpan.FromMilliseconds(500);

        private readonly EFCoreOrchestrationOptions _options;
        private readonly Func<OrchestrationDbContext> _dbContextFactory;
        private readonly OrchestratorMessageMapper _orchestratorMessageMapper;
        private readonly InstanceMapper _instanceMapper;
        private readonly ExecutionMapper _executionMapper;

        public EFCoreOrchestrationServiceClient(
            IOptions<EFCoreOrchestrationOptions> options,
            Func<OrchestrationDbContext> dbContextFactory,
            OrchestratorMessageMapper orchestratorMessageMapper,
            InstanceMapper instanceMapper,
            ExecutionMapper executionMapper)
        {
            _options = options.Value;
            _dbContextFactory = dbContextFactory;
            _orchestratorMessageMapper = orchestratorMessageMapper;
            _instanceMapper = instanceMapper;
            _executionMapper = executionMapper;
        }

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

                if (instance != null)
                {
                    throw new Exception($"Orchestration with id {instanceId} already exists");
                }

                instance = _instanceMapper.CreateInstance(executionStartedEvent);

                var runtimeState = new OrchestrationRuntimeState(new[] { executionStartedEvent });
                var execution = _executionMapper.CreateExecution(runtimeState);

                await dbContext.Instances.AddAsync(instance);
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

        protected abstract Task PurgeOrchestrationHistoryAsync(OrchestrationDbContext dbContext, DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType);

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

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(timeout);

                while (!cts.Token.IsCancellationRequested)
                {
                    var status = await GetInstanceStatus(instanceId);

                    if (status != null &&
                        status != OrchestrationStatus.Running &&
                        status != OrchestrationStatus.Pending &&
                        status != OrchestrationStatus.ContinuedAsNew)
                    {
                        return await GetOrchestrationStateAsync(instanceId, executionId);
                    }

                    await Task.Delay(_statusPollingInterval, cts.Token);
                }
            }

            return null;
        }

        private async Task<OrchestrationStatus?> GetInstanceStatus(string instanceId)
        {
            using (var dbContext = _dbContextFactory())
            {
                return await dbContext.Executions
                    .Where(e => e.InstanceId == instanceId && e.Instance.LastExecutionId == e.ExecutionId)
                    .Select(e => (OrchestrationStatus?)e.Status)
                    .FirstOrDefaultAsync();
            }
        }

        public IList<OrchestrationFeature> Features { get; } = new OrchestrationFeature[]
        {
            OrchestrationFeature.SearchByInstanceId,
            OrchestrationFeature.SearchByName,
            OrchestrationFeature.SearchByCreatedTime,
            OrchestrationFeature.SearchByStatus,
            OrchestrationFeature.QueryCount
        };

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

                var continuationToken = ContinuationToken.Parse(query.ContinuationToken);
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
                        ? new ContinuationToken
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
                var instance = await dbContext.Instances.FindAsync(instanceId);

                dbContext.Instances.Remove(instance);

                var deletedRows = await dbContext.SaveChangesAsync();

                return new PurgeInstanceHistoryResult
                {
                    InstancesDeleted = deletedRows > 0 ? 1 : 0
                };
            }
        }

        private class ContinuationToken
        {
            public long Index { get; set; }
            public long Count { get; set; }
            public DateTime CreatedTime { get; set; }
            public string InstanceId { get; set; }

            public static ContinuationToken Parse(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return null;

                return JsonConvert.DeserializeObject<ContinuationToken>(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
            }

            public string Serialize()
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)));
            }
        }
    }
}
