using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Server
{
    [Collection("AzureStorage")]
    public class AzureStorageServerTests : ServerTestsBase
    {
        public AzureStorageServerTests(ITestOutputHelper output) : base(output)
        {
            FastWaitTimeout *= 2;
            SlowWaitTimeout *= 2;
            SupportsMultipleExecutionStorage = false;
            SupportsTags = false;
        }

        protected override void ConfigureServerStorage(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("AzureStorageAccount");

            Skip.If(string.IsNullOrWhiteSpace(connectionString), "AzureStorageAccount connection string not configured");

            services.AddDurableTaskAzureStorage(options =>
            {
                options.TaskHubName = "test";
                options.StorageConnectionString = connectionString;
            });
        }

        protected override void ConigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConigureWorker(builder);

            builder.HasAllOrchestrations = true;
            builder.HasAllActivities = true;
        }
    }
}