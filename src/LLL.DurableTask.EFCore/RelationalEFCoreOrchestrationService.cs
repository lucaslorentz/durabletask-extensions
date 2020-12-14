using System;
using System.Data;
using System.Threading.Tasks;
using LLL.DurableTask.EFCore.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore
{
    public abstract class RelationalEFCoreOrchestrationService : EFCoreOrchestrationService
    {
        private const IsolationLevel TransactionIsolationLevel = IsolationLevel.ReadCommitted;

        protected RelationalEFCoreOrchestrationService(
            IOptions<EFCoreOrchestrationOptions> options,
            Func<OrchestrationDbContext> dbContextFactory,
            OrchestratorMessageMapper orchestratorMessageMapper,
            ActivityMessageMapper activityMessageMapper,
            InstanceMapper instanceMapper,
            ExecutionMapper executionMapper,
            ILogger<EFCoreOrchestrationService> logger)
            : base(options, dbContextFactory, orchestratorMessageMapper, activityMessageMapper, instanceMapper, executionMapper, logger)
        {
        }

        protected override async Task Migrate(OrchestrationDbContext dbContext)
        {
            await dbContext.Database.MigrateAsync();
        }

        protected override async Task<IDbContextTransaction> BeginLockTransaction(OrchestrationDbContext dbContext)
        {
            return await dbContext.Database.BeginTransactionAsync(TransactionIsolationLevel);
        }

        protected override async Task<int> RenewActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId, DateTime newLockedUntilUTC)
        {
            return await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE ActivityMessages SET AvailableAt = {newLockedUntilUTC} WHERE Id = {id} AND LockId = {lockId}");
        }

        protected override async Task<int> ReleaseActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId)
        {
            return await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE ActivityMessages SET AvailableAt = {DateTime.UtcNow} WHERE Id = {id} AND LockId = {lockId}");
        }
    }
}
