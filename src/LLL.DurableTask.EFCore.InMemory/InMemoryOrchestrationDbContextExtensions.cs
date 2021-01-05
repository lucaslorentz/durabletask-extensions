using System;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;

namespace LLL.DurableTask.EFCore.InMemory
{
    public class InMemoryOrchestrationDbContextExtensions : OrchestrationDbContextExtensions
    {
        private readonly object _lock = new object();

        public override Task Migrate(OrchestrationDbContext dbContext)
        {
            return Task.CompletedTask;
        }

        public override Task<Instance> TryLockNextInstanceAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout)
        {
            lock (_lock)
            {
                var instance = (
                    from message in dbContext.OrchestrationMessages
                    where message.AvailableAt <= DateTime.UtcNow
                    && message.Instance.LockedUntil <= DateTime.UtcNow
                    orderby message.AvailableAt
                    select message.Instance
                ).FirstOrDefault();

                if (instance == null)
                    return Task.FromResult(default(Instance));

                instance.LockId = Guid.NewGuid().ToString();
                instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                dbContext.SaveChanges();

                return Task.FromResult(instance);
            }
        }

        public override Task<Instance> TryLockNextInstanceAsync(
            OrchestrationDbContext dbContext,
            string[] queues,
            TimeSpan lockTimeout)
        {
            lock (_lock)
            {
                var instance = (
                    from message in dbContext.OrchestrationMessages
                    where message.AvailableAt <= DateTime.UtcNow
                    && queues.Contains(message.Queue)
                    && message.Instance.LockedUntil <= DateTime.UtcNow
                    orderby message.AvailableAt
                    select message.Instance
                ).FirstOrDefault();

                if (instance == null)
                    return Task.FromResult(default(Instance));

                instance.LockId = Guid.NewGuid().ToString();
                instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                dbContext.SaveChanges();

                return Task.FromResult(instance);
            }
        }

        public override Task<ActivityMessage> TryLockNextActivityMessageAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout)
        {
            lock (_lock)
            {
                var activityMessage = (
                    from message in dbContext.ActivityMessages
                    where message.LockedUntil <= DateTime.UtcNow
                    && message.Instance.LockedUntil <= DateTime.UtcNow
                    orderby message.LockedUntil
                    select message
                ).FirstOrDefault();

                if (activityMessage == null)
                    return Task.FromResult(default(ActivityMessage));

                activityMessage.LockId = Guid.NewGuid().ToString();
                activityMessage.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                dbContext.SaveChanges();

                return Task.FromResult(activityMessage);
            }
        }

        public override Task<ActivityMessage> TryLockNextActivityMessageAsync(
            OrchestrationDbContext dbContext,
            string[] queues,
            TimeSpan lockTimeout)
        {
            lock (_lock)
            {
                var activityMessage = (
                    from message in dbContext.ActivityMessages
                    where message.LockedUntil <= DateTime.UtcNow
                    && queues.Contains(message.Queue)
                    orderby message.LockedUntil
                    select message
                ).FirstOrDefault();

                if (activityMessage == null)
                    return Task.FromResult(default(ActivityMessage));

                activityMessage.LockId = Guid.NewGuid().ToString();
                activityMessage.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
                dbContext.SaveChanges();

                return Task.FromResult(activityMessage);
            }
        }

        public override Task PurgeOrchestrationHistoryAsync(
            OrchestrationDbContext dbContext,
            DateTime thresholdDateTimeUtc,
            OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            var executions = timeRangeFilterType switch
            {
                OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter =>
                    dbContext.Executions.Where(e => e.CreatedTime < thresholdDateTimeUtc).ToArray(),
                OrchestrationStateTimeRangeFilterType.OrchestrationLastUpdatedTimeFilter =>
                    dbContext.Executions.Where(e => e.LastUpdatedTime < thresholdDateTimeUtc).ToArray(),
                OrchestrationStateTimeRangeFilterType.OrchestrationCompletedTimeFilter =>
                    dbContext.Executions.Where(e => e.CompletedTime < thresholdDateTimeUtc).ToArray(),
                _ => throw new NotImplementedException()
            };

            dbContext.Executions.RemoveRange(executions);
            dbContext.SaveChanges();

            return Task.CompletedTask;
        }

        public override Task<int> PurgeInstanceHistoryAsync(
            OrchestrationDbContext dbContext,
            string instanceId)
        {
            var instance = dbContext.Instances.Find(instanceId);
            if (instance == null)
                return Task.FromResult(0);

            dbContext.Instances.Remove(instance);
            dbContext.SaveChanges();

            return Task.FromResult(1);
        }
    }
}