using System;
using LLL.DurableTask.EFCore.Mappers;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.SqlServer
{
    public class SqlServerEFCoreOrchestrationServiceClient : RelationalEFCoreOrchestrationServiceClient
    {
        public SqlServerEFCoreOrchestrationServiceClient(
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
