using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.AzureStorage.Tracking;
using LLL.DurableTask.Core;

namespace LLL.DurableTaskExtensions.AzureStorage
{
    public class AzureStorageOrchestrationServiceSearchClient : IOrchestrationServiceSearchClient
    {
        private readonly AzureStorageOrchestrationService _azureStorageOrchestrationService;

        public AzureStorageOrchestrationServiceSearchClient(
            AzureStorageOrchestrationService azureStorageOrchestrationService)
        {
            _azureStorageOrchestrationService = azureStorageOrchestrationService;
        }

        public async Task<OrchestrationQueryResult> GetOrchestrationsAsync(
            OrchestrationQuery query,
            CancellationToken cancellationToken = default)
        {
            var queryCondition = new OrchestrationInstanceStatusQueryCondition
            {
                InstanceIdPrefix = query.InstanceId,
                CreatedTimeFrom = query.CreatedTimeFrom ?? default,
                CreatedTimeTo = query.CreatedTimeTo ?? default,
                RuntimeStatus = query.RuntimeStatus,
            };

            var azureQueryResult = await _azureStorageOrchestrationService.GetOrchestrationStateAsync(queryCondition, query.Top, query.ContinuationToken);

            var queryResult = new OrchestrationQueryResult
            {
                Orchestrations = azureQueryResult.OrchestrationState.ToArray(),
                ContinuationToken = azureQueryResult.ContinuationToken,
            };

            return queryResult;
        }
    }
}
