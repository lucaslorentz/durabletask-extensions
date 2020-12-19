using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.Core
{
    public interface IExtendedOrchestrationServiceClient
    {
        Task<OrchestrationFeature[]> GetFeatures();

        Task<OrchestrationQueryResult> GetOrchestrationsAsync(OrchestrationQuery query, CancellationToken cancellationToken = default);

        Task<PurgeInstanceHistoryResult> PurgeInstanceHistoryAsync(string instanceId);
    }
}
