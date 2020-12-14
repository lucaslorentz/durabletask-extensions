using System;
using LLL.DurableTask.EFCore.Mappers;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.MySql
{
    public class MySqlEFCoreOrchestrationServiceClient : RelationalEFCoreOrchestrationServiceClient
    {
        public MySqlEFCoreOrchestrationServiceClient(
            IOptions<EFCoreOrchestrationOptions> options,
            Func<OrchestrationDbContext> dbContextFactory,
            OrchestratorMessageMapper orchestratorMessageMapper,
            InstanceMapper instanceMapper,
            ExecutionMapper executionMapper)
            : base(options, dbContextFactory, orchestratorMessageMapper, instanceMapper, executionMapper)
        {
        }
    }
}
