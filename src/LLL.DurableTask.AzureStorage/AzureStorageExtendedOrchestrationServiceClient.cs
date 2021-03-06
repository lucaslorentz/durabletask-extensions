﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.AzureStorage.Tracking;
using LLL.DurableTask.Core;

namespace LLL.DurableTaskExtensions.AzureStorage
{
    public class AzureStorageExtendedOrchestrationServiceClient : IExtendedOrchestrationServiceClient
    {
        private readonly AzureStorageOrchestrationService _azureStorageOrchestrationService;

        public AzureStorageExtendedOrchestrationServiceClient(
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

        public async Task<OrchestrationQueryResult> GetOrchestrationsAsync(
            OrchestrationQuery query,
            CancellationToken cancellationToken = default)
        {
            var queryCondition = new OrchestrationInstanceStatusQueryCondition
            {
                InstanceIdPrefix = query.InstanceId,
                CreatedTimeFrom = query.CreatedTimeFrom ?? default,
                CreatedTimeTo = query.CreatedTimeTo ?? default,
                RuntimeStatus = query.RuntimeStatus
            };

            var azureQueryResult = await _azureStorageOrchestrationService.GetOrchestrationStateAsync(queryCondition, query.Top, query.ContinuationToken);

            var queryResult = new OrchestrationQueryResult
            {
                Orchestrations = azureQueryResult.OrchestrationState.ToArray(),
                ContinuationToken = azureQueryResult.ContinuationToken,
            };

            return queryResult;
        }

        public async Task<DurableTask.Core.PurgeInstanceHistoryResult> PurgeInstanceHistoryAsync(string instanceId)
        {
            var azureStorageResult = await _azureStorageOrchestrationService.PurgeInstanceHistoryAsync(instanceId);

            return new DurableTask.Core.PurgeInstanceHistoryResult
            {
                InstancesDeleted = azureStorageResult.InstancesDeleted
            };
        }

        public async Task RewindTaskOrchestrationAsync(string instanceId, string reason)
        {
            await _azureStorageOrchestrationService.RewindTaskOrchestrationAsync(instanceId, reason);
        }
    }
}
