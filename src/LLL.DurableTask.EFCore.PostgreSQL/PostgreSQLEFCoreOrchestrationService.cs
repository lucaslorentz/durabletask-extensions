using System;
using System.Linq;
using System.Threading.Tasks;
using LLL.DurableTask.EFCore.Entities;
using LLL.DurableTask.EFCore.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.PostgreSQL
{
    public class PostgreSQLEFCoreOrchestrationService : RelationalEFCoreOrchestrationService
    {
        public PostgreSQLEFCoreOrchestrationService(
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

        protected override async Task<ActivityMessage> LockAnyQueueActivityMessage(OrchestrationDbContext dbContext)
        {
            return (await dbContext.ActivityMessages.FromSqlRaw(@"
                SELECT * FROM ""ActivityMessages""
                WHERE ""AvailableAt"" <= {0}
                ORDER BY ""AvailableAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<ActivityMessage> LockQueueActivityMessage(OrchestrationDbContext dbContext, string queue)
        {
            return (await dbContext.ActivityMessages.FromSqlRaw(@"
                SELECT * FROM ""ActivityMessages""
                WHERE ""Queue"" = {0}
                    AND ""AvailableAt"" <= {1}
                ORDER BY ""AvailableAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", queue, DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<Instance> LockAnyQueueInstance(OrchestrationDbContext dbContext)
        {
            return (await dbContext.Instances.FromSqlRaw(@"
                SELECT ""Instances"".* FROM ""OrchestratorMessages""
	                INNER JOIN ""Instances"" ON ""OrchestratorMessages"".""InstanceId"" = ""Instances"".""InstanceId""
                WHERE
                    ""Instances"".""AvailableAt"" <= {0}
	                AND ""OrchestratorMessages"".""AvailableAt"" <= {0}
                ORDER BY ""OrchestratorMessages"".""AvailableAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<Instance> LockQueueInstance(OrchestrationDbContext dbContext, string queue)
        {
            return (await dbContext.Instances.FromSqlRaw(@"
                SELECT ""Instances"".* FROM ""OrchestratorMessages""
	                INNER JOIN ""Instances"" ON ""OrchestratorMessages"".""InstanceId"" = ""Instances"".""InstanceId""
                WHERE
                    ""Instances"".""Queue"" = {0}
                    AND ""Instances"".""AvailableAt"" <= {1}
	                AND ""OrchestratorMessages"".""AvailableAt"" <= {1}
                ORDER BY ""OrchestratorMessages"".""AvailableAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", queue, DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }
    }
}
