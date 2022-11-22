using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.Core
{
    public interface IOrchestrationServiceSearchClient
    {
        Task<OrchestrationQueryResult> GetOrchestrationsAsync(OrchestrationQuery query, CancellationToken cancellationToken = default);
    }
}
