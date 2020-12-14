using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.Core
{
    public interface IExtendedOrchestrationServiceClient
    {
        IList<OrchestrationFeature> Features { get; }

        Task<OrchestrationQueryResult> GetOrchestrationsAsync(OrchestrationQuery query, CancellationToken cancellationToken = default);

        Task<PurgeInstanceHistoryResult> PurgeInstanceHistoryAsync(string instanceId);
    }
}
