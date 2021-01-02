using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LLL.DurableTask.EFCore.SqlServer
{
    public class SqlServerOrchestrationDbContextExtensions : OrchestrationDbContextExtensions
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
                SELECT TOP 1 Instances.* FROM OrchestrationMessages
                    INNER JOIN Instances WITH (UPDLOCK, READPAST, FORCESEEK)
                        ON OrchestrationMessages.InstanceId = Instances.InstanceId
                WHERE
                    OrchestrationMessages.AvailableAt <= {0}
                    AND Instances.AvailableAt <= {0}
                ORDER BY OrchestrationMessages.AvailableAt
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        public override async Task<Instance> LockNextInstanceForUpdate(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.Instances.FromSqlRaw($@"
                SELECT TOP 1 Instances.* FROM OrchestrationMessages
                    INNER JOIN Instances WITH (UPDLOCK, READPAST, FORCESEEK)
                        ON OrchestrationMessages.InstanceId = Instances.InstanceId
                WHERE
                    OrchestrationMessages.AvailableAt <= {utcNowParam}
                    AND OrchestrationMessages.Queue IN ({queuesParams})
                    AND Instances.AvailableAt <= {utcNowParam}
                ORDER BY OrchestrationMessages.AvailableAt
            ", parameters).ToArrayAsync()).FirstOrDefault();
        }

        public override async Task<ActivityMessage> LockNextActivityMessageForUpdate(OrchestrationDbContext dbContext)
        {
            return (await dbContext.ActivityMessages.FromSqlRaw(@"
                SELECT TOP 1 * FROM ActivityMessages WITH (UPDLOCK, READPAST)
                WHERE AvailableAt <= {0}
                ORDER BY AvailableAt
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        public override async Task<ActivityMessage> LockNextActivityMessageForUpdate(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.ActivityMessages.FromSqlRaw($@"
                SELECT TOP 1 * FROM ActivityMessages
                WITH (UPDLOCK, READPAST)
                WHERE Queue IN ({queuesParams})
                    AND AvailableAt <= {utcNowParam}
                ORDER BY AvailableAt
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
