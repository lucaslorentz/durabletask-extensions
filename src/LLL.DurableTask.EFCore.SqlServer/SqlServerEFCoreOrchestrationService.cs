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
            OrchestrationMessageMapper orchestratorMessageMapper,
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
                SELECT TOP 1 instances.* FROM orchestratormessages
                    INNER JOIN instances WITH (UPDLOCK, READPAST) ON orchestratormessages.instanceid = instances.instanceid
                WHERE
                    orchestratormessages.availableat <= {0}
                    AND instances.availableat <= {0}
                ORDER BY orchestratormessages.availableat
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<Instance> LockQueuesInstance(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.Instances.FromSqlRaw($@"
                SELECT TOP 1 instances.* FROM orchestratormessages
                    INNER JOIN instances WITH (UPDLOCK, READPAST) ON orchestratormessages.instanceid = instances.instanceid
                WHERE
                    orchestratormessages.availableat <= {utcNowParam}
                    AND instances.queue IN ({queuesParams})
                    AND instances.availableat <= {utcNowParam}
                ORDER BY orchestratormessages.availableat
            ", parameters).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<ActivityMessage> LockAnyQueueActivityMessage(OrchestrationDbContext dbContext)
        {
            return (await dbContext.ActivityMessages.FromSqlRaw(@"
                SELECT TOP 1 * FROM activitymessages WITH (UPDLOCK, READPAST)
                WHERE availableat <= {0}
                ORDER BY AvailableAt
            ", DateTime.UtcNow).ToArrayAsync()).FirstOrDefault();
        }

        protected override async Task<ActivityMessage> LockQueuesActivityMessage(OrchestrationDbContext dbContext, string[] queues)
        {
            var queuesParams = string.Join(",", queues.Select((_, i) => $"{{{i}}}"));
            var utcNowParam = $"{{{queues.Length}}}";
            var parameters = queues.Cast<object>().Concat(new object[] { DateTime.UtcNow }).ToArray();

            return (await dbContext.ActivityMessages.FromSqlRaw($@"
                SELECT TOP 1 * FROM activitymessages
                WITH (UPDLOCK, READPAST)
                WHERE queue IN ({queuesParams})
                    AND availableat <= {utcNowParam}
                ORDER BY AvailableAt
            ", parameters).ToArrayAsync()).FirstOrDefault();
        }
    }
}
