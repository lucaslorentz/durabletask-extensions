using DurableTask.AzureStorage;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StoragesBenchmark;

public class AzureStorageBenchmark : OrchestrationBenchmark
{
    protected override void ConfigureStorage(IServiceCollection services)
    {
        var connectionString = _configuration.GetConnectionString("AzureStorageAccount");

        services.AddDurableTaskAzureStorage(options =>
        {
            options.TaskHubName = "test";
            options.StorageAccountClientProvider = new StorageAccountClientProvider(connectionString);
        });
    }

    protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
    {
        base.ConfigureWorker(builder);
        builder.HasAllOrchestrations = true;
        builder.HasAllActivities = true;
    }
}
