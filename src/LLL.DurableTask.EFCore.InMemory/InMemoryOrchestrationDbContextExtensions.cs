using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;

namespace LLL.DurableTask.EFCore.InMemory
{
    public class InMemoryOrchestrationDbContextExtensions : OrchestrationDbContextExtensions
    {
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _instancesSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly ConcurrentDictionary<OrchestrationDbContext, HashSet<string>> _lockedInstanes = new ConcurrentDictionary<OrchestrationDbContext, HashSet<string>>();

        public override Task Migrate(OrchestrationDbContext dbContext)
        {
            return Task.CompletedTask;
        }

        public override async Task WithinTransaction(OrchestrationDbContext dbContext, Func<Task> action)
        {
            await action();
        }

        public override async Task<Instance> LockInstanceForUpdate(OrchestrationDbContext dbContext, string instanceId)
        {
            var instance = dbContext.Instances.Find(instanceId);

            if (instance == null)
                return null;

            var lockedInstances = _lockedInstanes.GetOrAdd(dbContext, (d) => new HashSet<string>());
            if (!lockedInstances.Add(instanceId))
                return instance;

            var semaphore = _instancesSemaphores.GetOrAdd(instanceId, (_) => new SemaphoreSlim(1));
            await semaphore.WaitAsync();

            dbContext.SaveChangesFailed += (o, e) => Unlock();
            dbContext.SavedChanges += (o, e) => Unlock();

            return instance;

            void Unlock()
            {
                semaphore.Release();
                lockedInstances.Remove(instanceId);
            }
        }

        public override Task<Instance> TryLockNextInstanceAsync(
            OrchestrationDbContext dbContext,
            TimeSpan lockTimeout)
        {
            lock (_lock)
            {
                var instance = (
                    from b in dbContext.OrchestrationMessages
                    where b.AvailableAt <= DateTime.UtcNow
                    && b.Instance.LockedUntil <= DateTime.UtcNow
                    orderby b.AvailableAt
                    select b.Instance
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
                    from b in dbContext.OrchestrationMessages
                    where b.AvailableAt <= DateTime.UtcNow
                    && queues.Contains(b.Queue)
                    && b.Instance.LockedUntil <= DateTime.UtcNow
                    orderby b.AvailableAt
                    select b.Instance
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
            var executions = dbContext.Executions.Where(e => e.InstanceId == instanceId).ToArray();
            dbContext.Executions.RemoveRange(executions);
            dbContext.SaveChanges();
            return Task.FromResult(executions.Length);
        }
    }
}