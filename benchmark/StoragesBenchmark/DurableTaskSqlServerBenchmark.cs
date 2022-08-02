using DurableTask.Core;
using DurableTask.SqlServer;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StoragesBenchmark
{
    public class DurableTaskSqlServerBenchmark : OrchestrationBenchmark
    {
        protected override void ConfigureStorage(IServiceCollection services)
        {
            var connectionString = _configuration.GetConnectionString("SqlServer");

            var settings = new SqlOrchestrationServiceSettings(connectionString);

            var provider = new SqlOrchestrationService(settings);
            provider.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            services.AddSingleton<SqlOrchestrationService>(provider);
            services.AddSingleton<IOrchestrationService>(p => p.GetRequiredService<SqlOrchestrationService>());
            services.AddSingleton<IOrchestrationServiceClient>(p => p.GetRequiredService<SqlOrchestrationService>());
        }

        protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConfigureWorker(builder);
            builder.HasAllOrchestrations = true;
            builder.HasAllActivities = true;
        }
    }
}
