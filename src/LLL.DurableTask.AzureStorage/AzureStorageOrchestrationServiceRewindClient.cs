using System.Threading.Tasks;
using DurableTask.AzureStorage;
using LLL.DurableTask.Core;

namespace LLL.DurableTaskExtensions.AzureStorage;

public class AzureStorageOrchestrationServiceRewindClient : IOrchestrationServiceRewindClient
{
    private readonly AzureStorageOrchestrationService _azureStorageOrchestrationService;

    public AzureStorageOrchestrationServiceRewindClient(
        AzureStorageOrchestrationService azureStorageOrchestrationService)
    {
        _azureStorageOrchestrationService = azureStorageOrchestrationService;
    }

    public async Task RewindTaskOrchestrationAsync(string instanceId, string reason)
    {
        await _azureStorageOrchestrationService.RewindTaskOrchestrationAsync(instanceId, reason);
    }
}
