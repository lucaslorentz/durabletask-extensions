using System;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace LLL.DurableTask.EFCore
{
    public abstract class OrchestrationDbContextExtensions
    {
        public abstract Task Migrate(OrchestrationDbContext dbContext);

        public abstract Task<IDbContextTransaction> BeginTransaction(OrchestrationDbContext dbContext);

        public abstract Task<Instance> LockNextInstanceForUpdate(OrchestrationDbContext dbContext);

        public abstract Task<Instance> LockNextInstanceForUpdate(OrchestrationDbContext dbContext, string[] queues);

        public abstract Task<ActivityMessage> LockNextActivityMessageForUpdate(OrchestrationDbContext dbContext);

        public abstract Task<ActivityMessage> LockNextActivityMessageForUpdate(OrchestrationDbContext dbContext, string[] queues);

        public abstract Task<int> RenewActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId, DateTime lockedUntilUTC);

        public abstract Task<int> ReleaseActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId);

        public abstract Task PurgeOrchestrationHistoryAsync(OrchestrationDbContext dbContext, DateTime thresholdDateTimeUtc, OrchestrationStateTimeRangeFilterType timeRangeFilterType);

        public abstract Task<int> PurgeInstanceHistoryAsync(OrchestrationDbContext dbContext, string instanceId);
    }
}