using System;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;

namespace LLL.DurableTask.EFCore
{
    public abstract class OrchestrationDbContextExtensions
    {
        public abstract Task Migrate(OrchestrationDbContext dbContext);

        public abstract Task WithinTransaction(OrchestrationDbContext dbContext, Func<Task> action);

        public abstract Task LockInstance(
            OrchestrationDbContext dbContext,
            string instanceId);

        public abstract Task<OrchestrationBatch> TryLockNextOrchestrationBatchAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout);

        public abstract Task<OrchestrationBatch> TryLockNextOrchestrationBatchAsync(
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

        public abstract Task PurgeOrchestrationHistoryAsync(OrchestrationDbContext dbContext, DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType);

        public abstract Task<int> PurgeInstanceHistoryAsync(OrchestrationDbContext dbContext, string instanceId);
    }
}