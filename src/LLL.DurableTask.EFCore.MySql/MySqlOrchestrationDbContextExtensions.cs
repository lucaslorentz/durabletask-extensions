using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LLL.DurableTask.EFCore.MySql
{
    public class MySqlOrchestrationDbContextExtensions : OrchestrationDbContextExtensions
    {
        private const IsolationLevel TransactionIsolationLevel = IsolationLevel.ReadCommitted;

        public override async Task Migrate(OrchestrationDbContext dbContext)
        {
            await dbContext.Database.MigrateAsync();
        }

        public override async Task<IDbContextTransaction> BeginTransaction(OrchestrationDbContext dbContext)
        {
            return await dbContext.Database.BeginTransactionAsync(TransactionIsolationLevel);
        }

        public override async Task<Instance> LockNextInstanceForUpdate(OrchestrationDbContext dbContext)
        {
            return (await dbContext.Instances.FromSqlRaw(@"
                SELECT Instances.* FROM OrchestrationMessages
	                STRAIGHT_JOIN Instances FORCE INDEX FOR JOIN (PRIMARY)
                        ON OrchestrationMessages.InstanceId = Instances.InstanceId
                WHERE
                    OrchestrationMessages.AvailableAt <= {0}
                    AND Instances.AvailableAt <= {0}
                ORDER BY OrchestrationMessages.AvailableAt
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        public override async Task<Instance> LockNextInstanceForUpdate(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.Instances.FromSqlRaw($@"
                SELECT Instances.* FROM OrchestrationMessages
	                STRAIGHT_JOIN Instances FORCE INDEX FOR JOIN (PRIMARY)
                        ON OrchestrationMessages.InstanceId = Instances.InstanceId
                WHERE
                    OrchestrationMessages.AvailableAt <= {utcNowParam}
                    AND OrchestrationMessages.Queue IN ({queuesParams})
                    AND Instances.AvailableAt <= {utcNowParam}
                ORDER BY OrchestrationMessages.AvailableAt
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", parameters).ToArrayAsync()).FirstOrDefault();
        }

        public override async Task<ActivityMessage> LockNextActivityMessageForUpdate(OrchestrationDbContext dbContext)
        {
            return (await dbContext.ActivityMessages.FromSqlRaw(@"
                SELECT * FROM ActivityMessages
                WHERE AvailableAt <= {0}
                ORDER BY AvailableAt
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        public override async Task<ActivityMessage> LockNextActivityMessageForUpdate(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.ActivityMessages.FromSqlRaw($@"
                SELECT * FROM ActivityMessages
                WHERE Queue IN ({queuesParams})
                    AND AvailableAt <= {utcNowParam}
                ORDER BY AvailableAt
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", parameters).ToArrayAsync()).FirstOrDefault();
        }

        public override async Task<int> RenewActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId, DateTime newLockedUntilUTC)
        {
            return await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE ActivityMessages SET AvailableAt = {newLockedUntilUTC} WHERE Id = {id} AND LockId = {lockId}");
        }

        public override async Task<int> ReleaseActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId)
        {
            return await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE ActivityMessages SET AvailableAt = {DateTime.UtcNow} WHERE Id = {id} AND LockId = {lockId}");
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