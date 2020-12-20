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

        protected override async Task<Instance> LockAnyQueueInstance(OrchestrationDbContext dbContext)
        {
            return (await dbContext.Instances.FromSqlRaw(@"
                SELECT ""Instances"".* FROM ""OrchestratorMessages""
	                INNER JOIN ""Instances"" ON ""OrchestratorMessages"".""InstanceId"" = ""Instances"".""InstanceId""
                WHERE
                    ""OrchestratorMessages"".""AvailableAt"" <= {0}
                    AND ""Instances"".""AvailableAt"" <= {0}
                ORDER BY ""OrchestratorMessages"".""AvailableAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<Instance> LockQueuesInstance(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.Instances.FromSqlRaw($@"
                SELECT ""Instances"".* FROM ""OrchestratorMessages""
	                INNER JOIN ""Instances"" ON ""OrchestratorMessages"".""InstanceId"" = ""Instances"".""InstanceId""
                WHERE
                    ""OrchestratorMessages"".""AvailableAt"" <= {utcNowParam}
                    AND ""Instances"".""Queue"" IN ({queuesParams})
                    AND ""Instances"".""AvailableAt"" <= {utcNowParam}
                ORDER BY ""OrchestratorMessages"".""AvailableAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", parameters).ToArrayAsync()).FirstOrDefault();
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

        protected override async Task<ActivityMessage> LockQueuesActivityMessage(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.ActivityMessages.FromSqlRaw($@"
                SELECT * FROM ""ActivityMessages""
                WHERE ""Queue"" IN ({queuesParams})
                    AND ""AvailableAt"" <= {utcNowParam}
                ORDER BY ""AvailableAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            ", parameters).ToArrayAsync()).FirstOrDefault();
        }
    }
}
