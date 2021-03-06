using System;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;

namespace LLL.DurableTask.EFCore
{
    public abstract class OrchestrationDbContextExtensions
    {
        public abstract Task Migrate(OrchestrationDbContext dbContext);

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

        public abstract Task PurgeOrchestrationHistoryAsync(OrchestrationDbContext dbContext, DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType);

        public abstract Task<int> PurgeInstanceHistoryAsync(OrchestrationDbContext dbContext, string instanceId);
    }
}