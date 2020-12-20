using System;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore
{
    public abstract class RelationalEFCoreOrchestrationServiceClient : EFCoreOrchestrationServiceClient
    {
        public RelationalEFCoreOrchestrationServiceClient(
            IOptions<EFCoreOrchestrationOptions> options,
            Func<OrchestrationDbContext> dbContextFactory,
            OrchestratorMessageMapper orchestratorMessageMapper,
            InstanceMapper instanceMapper,
            ExecutionMapper executionMapper)
            : base(options, dbContextFactory, orchestratorMessageMapper, instanceMapper, executionMapper)
        {
        }

        protected override async Task PurgeOrchestrationHistoryAsync(
            OrchestrationDbContext dbContext,
            DateTime thresholdDateTimeUtc,
            OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            switch (timeRangeFilterType)
            {
                case OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter:
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Execution WHERE CreatedTime < {thresholdDateTimeUtc}");
                    break;
                case OrchestrationStateTimeRangeFilterType.OrchestrationLastUpdatedTimeFilter:
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Execution WHERE LastUpdatedTime < {thresholdDateTimeUtc}");
                    break;
                case OrchestrationStateTimeRangeFilterType.OrchestrationCompletedTimeFilter:
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Execution WHERE CompletedTime < {thresholdDateTimeUtc}");
                    break;
            }
        }
    }
}
