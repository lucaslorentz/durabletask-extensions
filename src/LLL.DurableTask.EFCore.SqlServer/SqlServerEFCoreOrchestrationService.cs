using System;
using System.Linq;
using System.Threading.Tasks;
using LLL.DurableTask.EFCore.Entities;
using LLL.DurableTask.EFCore.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.SqlServer
{
    public class SqlServerEFCoreOrchestrationService : RelationalEFCoreOrchestrationService
    {
        public SqlServerEFCoreOrchestrationService(
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
                SELECT TOP 1 * FROM activitymessages WITH (UPDLOCK, READPAST)
                WHERE availableat <= {0}
                ORDER BY AvailableAt
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<ActivityMessage> LockQueueActivityMessage(OrchestrationDbContext dbContext, string queue)
        {
            return (await dbContext.ActivityMessages.FromSqlRaw(@"
                SELECT TOP 1 * FROM activitymessages WITH (UPDLOCK, READPAST)
                WHERE queue = {0}
                    AND availableat <= {1}
                ORDER BY AvailableAt
            ", queue, DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<Instance> LockAnyQueueInstance(OrchestrationDbContext dbContext)
        {
            return (await dbContext.Instances.FromSqlRaw(@"
                SELECT TOP 1 instances.* FROM orchestratormessages
                    INNER JOIN instances WITH (UPDLOCK, READPAST) ON orchestratormessages.instanceid = instances.instanceid
                WHERE
                    instances.availableat <= {0}
                    AND orchestratormessages.availableat <= {0}
                ORDER BY orchestratormessages.availableat
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<Instance> LockQueueInstance(OrchestrationDbContext dbContext, string queue)
        {
            return (await dbContext.Instances.FromSqlRaw(@"
                SELECT TOP 1 instances.* FROM orchestratormessages
                    INNER JOIN instances WITH (UPDLOCK, READPAST) ON orchestratormessages.instanceid = instances.instanceid
                WHERE
                    instances.queue = {0}
                    AND instances.availableat <= {1}
                    AND orchestratormessages.availableat <= {1}
                ORDER BY orchestratormessages.availableat
            ", queue, DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }
    }
}
