using System;
using DurableTask.AzureStorage;
using DurableTask.Core;
using DurableTask.Core.Query;
using LLL.DurableTask.Core;
using LLL.DurableTaskExtensions.AzureStorage;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class AzureStorageServiceCollectionExtensions
{
    public static IServiceCollection AddDurableTaskAzureStorage(
        this IServiceCollection services,
        Action<AzureStorageOrchestrationServiceSettings> configure)
    {
        services.AddOptions<AzureStorageOrchestrationServiceSettings>()
            .Configure(configure);

        services.AddTransient(p => p.GetRequiredService<IOptions<AzureStorageOrchestrationServiceSettings>>().Value);

        services.AddSingleton<AzureStorageOrchestrationService>();
        services.AddSingleton<AzureStorageOrchestrationServiceRewindClient>();

        services.AddSingleton<IOrchestrationService>(p => p.GetService<AzureStorageOrchestrationService>());
        services.AddSingleton<IOrchestrationServiceClient>(p => p.GetService<AzureStorageOrchestrationService>());
        services.AddSingleton<IOrchestrationServiceQueryClient>(p => p.GetService<AzureStorageOrchestrationService>());
        services.AddSingleton<IOrchestrationServicePurgeClient>(p => p.GetService<AzureStorageOrchestrationService>());
        services.AddSingleton<IOrchestrationServiceFeaturesClient>(p => p.GetService<AzureStorageOrchestrationServiceFeaturesClient>());
        services.AddSingleton<IOrchestrationServiceRewindClient>(p => p.GetService<AzureStorageOrchestrationServiceRewindClient>());

        return services;
    }
}
