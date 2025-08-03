using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Query;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore.MySql;

public class MySqlOrchestrationDbContextExtensions : OrchestrationDbContextExtensions
{
    private const IsolationLevel TransactionIsolationLevel = IsolationLevel.ReadCommitted;

    public override async Task Migrate(OrchestrationDbContext dbContext)
    {
        await dbContext.Database.MigrateAsync();
    }

    public override async Task WithinTransaction(OrchestrationDbContext dbContext, Func<Task> action)
    {
        using var transaction = dbContext.Database.BeginTransaction(TransactionIsolationLevel);
        await action();

        await transaction.CommitAsync();
    }

    public override async Task<Instance> LockInstanceForUpdate(OrchestrationDbContext dbContext, string instanceId)
    {
        return (await dbContext.Instances.FromSqlRaw(@"
                SELECT * FROM Instances
                WHERE InstanceId = {0}
                FOR UPDATE
            ", instanceId).ToArrayAsync()).FirstOrDefault();
    }

    public override async Task<Instance> TryLockNextInstanceAsync(
        OrchestrationDbContext dbContext,
        TimeSpan lockTimeout)
    {
        var instance = (await dbContext.Instances.FromSqlRaw(@"
                SELECT Instances.*
                FROM OrchestrationMessages FORCE INDEX (IX_OrchestrationMessages_AvailableAt_Queue_InstanceId)
                    INNER JOIN Instances FORCE INDEX (IX_Instances_InstanceId_LockedUntil)
                        ON OrchestrationMessages.InstanceId = Instances.InstanceId
                WHERE
                    OrchestrationMessages.AvailableAt <= {0}
                    AND Instances.LockedUntil <= {0}
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", DateTime.UtcNow).WithStraightJoin().ToArrayAsync()).FirstOrDefault();

        if (instance is null)
            return null;

        instance.LockId = Guid.NewGuid().ToString();
        instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
        await dbContext.SaveChangesAsync();

        return instance;
    }

    public override async Task<Instance> TryLockNextInstanceAsync(
        OrchestrationDbContext dbContext,
        string[] queues,
        TimeSpan lockTimeout)
    {
        var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
        var utcNowParam = $"{{{queues.Length}}}";
        var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

        var instance = (await dbContext.Instances.FromSqlRaw($@"
                SELECT Instances.*
                FROM OrchestrationMessages FORCE INDEX (IX_OrchestrationMessages_AvailableAt_Queue_InstanceId)
                    INNER JOIN Instances FORCE INDEX (IX_Instances_InstanceId_LockedUntil)
                        ON OrchestrationMessages.InstanceId = Instances.InstanceId
                WHERE
                    OrchestrationMessages.AvailableAt <= {utcNowParam}
                    AND OrchestrationMessages.Queue IN ({queuesParams})
                    AND Instances.LockedUntil <= {utcNowParam}
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", parameters).WithStraightJoin().ToArrayAsync()).FirstOrDefault();

        if (instance is null)
            return null;

        instance.LockId = Guid.NewGuid().ToString();
        instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
        await dbContext.SaveChangesAsync();

        return instance;
    }

    public override async Task<ActivityMessage> TryLockNextActivityMessageAsync(
        OrchestrationDbContext dbContext,
        TimeSpan lockTimeout)
    {
        var instance = (await dbContext.ActivityMessages.FromSqlRaw(@"
                SELECT *
                FROM ActivityMessages FORCE INDEX(IX_ActivityMessages_LockedUntil_Queue)
                WHERE LockedUntil <= {0}
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();

        if (instance is null)
            return null;

        instance.LockId = Guid.NewGuid().ToString();
        instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
        await dbContext.SaveChangesAsync();

        return instance;
    }

    public override async Task<ActivityMessage> TryLockNextActivityMessageAsync(
        OrchestrationDbContext dbContext,
        string[] queues,
        TimeSpan lockTimeout)
    {
        var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
        var utcNowParam = $"{{{queues.Length}}}";
        var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

        var instance = (await dbContext.ActivityMessages.FromSqlRaw($@"
                SELECT * FROM ActivityMessages FORCE INDEX(IX_ActivityMessages_LockedUntil_Queue)
                WHERE Queue IN ({queuesParams})
                    AND LockedUntil <= {utcNowParam}
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", parameters).ToArrayAsync()).FirstOrDefault();

        if (instance is null)
            return null;

        instance.LockId = Guid.NewGuid().ToString();
        instance.LockedUntil = DateTime.UtcNow.Add(lockTimeout);
        await dbContext.SaveChangesAsync();

        return instance;
    }

    public override IQueryable<Execution> CreateFilteredQueryable(
        OrchestrationDbContext dbContext,
        OrchestrationQuery query)
    {
        var queryable = base.CreateFilteredQueryable(dbContext, query);

        if (query is not OrchestrationQueryExtended extendedQuery
            || !extendedQuery.Tags.Any())
        {
            queryable = queryable.WithStraightJoin();
        }

        return queryable;
    }

    public override async Task<int> PurgeInstanceHistoryAsync(OrchestrationDbContext dbContext, PurgeInstanceFilter filter)
    {
        var limit = filter is PurgeInstanceFilterExtended filterExtended
            ? filterExtended.Limit
            : null;

        var parameters = new ParametersCollection();

        return await dbContext.Database.ExecuteSqlRawAsync($@"
            DELETE FROM Executions
            WHERE ExecutionId IN(
                SELECT ExecutionId FROM (
                    SELECT Executions.ExecutionId
                    FROM Executions
                        INNER JOIN Instances ON Executions.InstanceId = Instances.InstanceId
                    WHERE Executions.CreatedTime > {parameters.Add(filter.CreatedTimeFrom)}
                    {(filter.CreatedTimeTo is not null ? $"AND Executions.CreatedTime < {parameters.Add(filter.CreatedTimeTo)}" : "")}
                    {(filter.RuntimeStatus.Any() ? $"AND Executions.Status IN ({string.Join(",", filter.RuntimeStatus.Select(s => parameters.Add(s.ToString())))})" : "")}
                    ORDER BY Executions.CreatedTime
                    {(limit is not null ? $"LIMIT {parameters.Add(limit)}" : null)}
                    FOR UPDATE SKIP LOCKED
                ) T
            );
        ", parameters);
    }
}
