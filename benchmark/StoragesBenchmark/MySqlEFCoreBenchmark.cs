using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StoragesBenchmark
{
    public class MySqlEFCoreBenchmark : OrchestrationBenchmark
    {
        protected override void ConfigureStorage(IServiceCollection services)
        {
            var connectionString = _configuration.GetConnectionString("MySql");

            services.AddDurableTaskEFCoreStorage()
                .UseMySql(connectionString);
        }
    }
}
