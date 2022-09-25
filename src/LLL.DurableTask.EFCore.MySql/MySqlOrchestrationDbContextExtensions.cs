using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore.MySql
{
    public class MySqlOrchestrationDbContextExtensions : OrchestrationDbContextExtensions
    {
        private const IsolationLevel TransactionIsolationLevel = IsolationLevel.ReadCommitted;

        public override async Task Migrate(OrchestrationDbContext dbContext)
        {
            await dbContext.Database.MigrateAsync();
        }

        public override async Task WithinTransaction(OrchestrationDbContext dbContext, Func<Task> action)
        {
            using (var transaction = dbContext.Database.BeginTransaction(TransactionIsolationLevel))
            {
                await action();

                await transaction.CommitAsync();
            }
        }

        public override async Task<Instance> LockInstanceForUpdate(OrchestrationDbContext dbContext, string instanceId)
        {
            return await dbContext.Instances.FromSqlRaw(@"
                SELECT * FROM Instances
                WHERE InstanceId = {0}
                FOR UPDATE
            ", instanceId).SingleOrDefaultAsync();
        }

        public override async Task<Instance> TryLockNextInstanceAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout)
        {
            using (var transaction = dbContext.Database.BeginTransaction(TransactionIsolationLevel))
            {
                var instance = await dbContext.Instances.FromSqlRaw(@"
                    SELECT Instances.* FROM OrchestrationMessages
                        INNER JOIN Instances ON OrchestrationMessages.InstanceId = Instances.InstanceId
                    WHERE
                        OrchestrationMessages.AvailableAt <= {0}
                        AND Instances.LockedUntil <= {0}
                    ORDER BY OrchestrationMessages.AvailableAt
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                ", DateTime.UtcNow).SingleOrDefaultAsync();

                if (instance == null)
                    return null;

                instance.LockId = Guid.NewGuid().ToString();
                instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return instance;
            }
        }

        public override async Task<Instance> TryLockNextInstanceAsync(
            OrchestrationDbContext dbContext,
            string[] queues,
            TimeSpan lockTimeout)
        {
            using (var transaction = dbContext.Database.BeginTransaction(TransactionIsolationLevel))
            {
                var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
                var utcNowParam = $"{{{queues.Length}}}";
                var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

                var instance = await dbContext.Instances.FromSqlRaw($@"
                    SELECT Instances.* FROM OrchestrationMessages
                        INNER JOIN Instances ON OrchestrationMessages.InstanceId = Instances.InstanceId
                    WHERE
                        OrchestrationMessages.AvailableAt <= {utcNowParam}
                        AND OrchestrationMessages.Queue IN ({queuesParams})
                        AND Instances.LockedUntil <= {utcNowParam}
                    ORDER BY OrchestrationMessages.AvailableAt
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                ", parameters).SingleOrDefaultAsync();

                if (instance == null)
                    return null;

                instance.LockId = Guid.NewGuid().ToString();
                instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return instance;
            }
        }

        public override async Task<ActivityMessage> TryLockNextActivityMessageAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout)
        {
            using (var transaction = dbContext.Database.BeginTransaction(TransactionIsolationLevel))
            {
                var instance = await dbContext.ActivityMessages.FromSqlRaw(@"
                    SELECT * FROM ActivityMessages
                    WHERE LockedUntil <= {0}
                    ORDER BY LockedUntil
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                ", DateTime.UtcNow).SingleOrDefaultAsync();

                if (instance == null)
                    return null;

                instance.LockId = Guid.NewGuid().ToString();
                instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return instance;
            }
        }

        public override async Task<ActivityMessage> TryLockNextActivityMessageAsync(
            OrchestrationDbContext dbContext,
            string[] queues,
            TimeSpan lockTimeout)
        {
            using (var transaction = dbContext.Database.BeginTransaction(TransactionIsolationLevel))
            {
                var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
                var utcNowParam = $"{{{queues.Length}}}";
                var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

                var instance = await dbContext.ActivityMessages.FromSqlRaw($@"
                    SELECT * FROM ActivityMessages
                    WHERE Queue IN ({queuesParams})
                        AND LockedUntil <= {utcNowParam}
                    ORDER BY LockedUntil
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                ", parameters).SingleOrDefaultAsync();

                if (instance == null)
                    return null;

                instance.LockId = Guid.NewGuid().ToString();
                instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return instance;
            }
        }

        public override async Task PurgeOrchestrationHistoryAsync(
            OrchestrationDbContext dbContext,
            DateTime thresholdDateTimeUtc,
            OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            switch (timeRangeFilterType)
            {
                case OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter:
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Executions WHERE CreatedTime < {thresholdDateTimeUtc}");
                    break;
                case OrchestrationStateTimeRangeFilterType.OrchestrationLastUpdatedTimeFilter:
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Executions WHERE LastUpdatedTime < {thresholdDateTimeUtc}");
                    break;
                case OrchestrationStateTimeRangeFilterType.OrchestrationCompletedTimeFilter:
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Executions WHERE CompletedTime < {thresholdDateTimeUtc}");
                    break;
            }
        }

        public override async Task<int> PurgeInstanceHistoryAsync(
            OrchestrationDbContext dbContext,
            string instanceId)
        {
            return await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Executions WHERE InstanceId = {instanceId}");
        }
    }
}