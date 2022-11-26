using System;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
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
    }
}