using System;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Query;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore
{
    public abstract class OrchestrationDbContextExtensions
    {
        public abstract Task Migrate(OrchestrationDbContext dbContext);

        public abstract Task WithinTransaction(OrchestrationDbContext dbContext, Func<Task> action);

        public async Task<T> WithinTransaction<T>(OrchestrationDbContext dbContext, Func<Task<T>> action)
        {
            T result = default;
            await WithinTransaction(dbContext, async () =>
            {
                result = await action();
            });
            return result;
        }

        public abstract Task<Instance> LockInstanceForUpdate(
            OrchestrationDbContext dbContext,
            string instanceId);

        public abstract Task<Instance> TryLockNextInstanceAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout);

        public abstract Task<Instance> TryLockNextInstanceAsync(
            OrchestrationDbContext dbContext,
            string[] queues,
            TimeSpan lockTimeout);

        public abstract Task<ActivityMessage> TryLockNextActivityMessageAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout);

        public abstract Task<ActivityMessage> TryLockNextActivityMessageAsync(
            OrchestrationDbContext dbContext,
            string[] queues,
            TimeSpan lockTimeout);

        public async Task PurgeOrchestrationHistoryAsync(
            OrchestrationDbContext dbContext,
            DateTime thresholdDateTimeUtc,
            OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            var query = timeRangeFilterType switch
            {
                OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter =>
                    dbContext.Executions.Where(e => e.CreatedTime < thresholdDateTimeUtc),
                OrchestrationStateTimeRangeFilterType.OrchestrationLastUpdatedTimeFilter =>
                    dbContext.Executions.Where(e => e.LastUpdatedTime < thresholdDateTimeUtc),
                OrchestrationStateTimeRangeFilterType.OrchestrationCompletedTimeFilter =>
                    dbContext.Executions.Where(e => e.CompletedTime < thresholdDateTimeUtc),
                _ => throw new NotImplementedException()
            };

            await ExecuteDeleteAsync(dbContext, query);
        }

        public async Task<int> PurgeInstanceHistoryAsync(
            OrchestrationDbContext dbContext,
            string instanceId)
        {
            var query = dbContext.Executions.Where(e => e.InstanceId == instanceId);

            return await ExecuteDeleteAsync(dbContext, query);
        }

        public async Task<int> PurgeInstanceHistoryAsync(OrchestrationDbContext dbContext, PurgeInstanceFilter filter)
        {
            var query = dbContext.Executions.Where(e => e.CreatedTime >= filter.CreatedTimeFrom
                && (filter.CreatedTimeTo == null || e.CreatedTime <= filter.CreatedTimeTo)
                && (filter.RuntimeStatus == null || filter.RuntimeStatus.Contains(e.Status)));

            return await ExecuteDeleteAsync(dbContext, query);
        }

        protected virtual Task<int> ExecuteDeleteAsync<T>(OrchestrationDbContext dbContext, IQueryable<T> query)
            where T : class
        {
            return query.ExecuteDeleteAsync();
        }

        public virtual IQueryable<Execution> CreateFilteredQueryable(
            OrchestrationDbContext dbContext,
            OrchestrationQuery query)
        {
            var extendedQuery = query as ExtendedOrchestrationQuery;

            var queryable = dbContext.Executions as IQueryable<Entities.Execution>;

            if (extendedQuery != null && !extendedQuery.IncludePreviousExecutions)
                queryable = queryable.Join(dbContext.Instances, x => x.ExecutionId, x => x.LastExecutionId, (x, y) => x);

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
                {
                    var executionsWithTag = dbContext.Executions
                        .SelectMany(e => e.Tags, (e, t) => new { e, t })
                        .Where(e => e.t.Name == kv.Key && e.t.Value == kv.Value)
                        .Select(x => new { x.e.ExecutionId });

                    queryable = queryable.Join(
                        executionsWithTag,
                        x => x.ExecutionId,
                        x => x.ExecutionId,
                        (x, y) => x);
                }
            }

            var continuationToken = EFCoreContinuationToken.Parse(query.ContinuationToken);
            if (continuationToken != null)
            {
                queryable = queryable.Where(i =>
                    i.CreatedTime < continuationToken.CreatedTime ||
                    i.CreatedTime == continuationToken.CreatedTime && continuationToken.InstanceId.CompareTo(i.InstanceId) < 0);
            }

            return queryable;
        }
    }
}