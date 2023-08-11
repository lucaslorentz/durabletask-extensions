using System.Threading.Tasks;
using DurableTask.AzureStorage;
using LLL.DurableTask.Core;

namespace LLL.DurableTaskExtensions.AzureStorage;

public class AzureStorageOrchestrationServiceFeaturesClient : IOrchestrationServiceFeaturesClient
{
    private readonly AzureStorageOrchestrationService _azureStorageOrchestrationService;

    public AzureStorageOrchestrationServiceFeaturesClient(
        AzureStorageOrchestrationService azureStorageOrchestrationService)
    {
        _azureStorageOrchestrationService = azureStorageOrchestrationService;
    }

    public Task<OrchestrationFeature[]> GetFeatures()
    {
        return Task.FromResult(new OrchestrationFeature[]
        {
            OrchestrationFeature.SearchByInstanceId,
            OrchestrationFeature.SearchByCreatedTime,
            OrchestrationFeature.SearchByStatus,
            OrchestrationFeature.Rewind
        });
    }
}
