using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StoragesBenchmark
{
    public class PostgresEFCoreBenchmark : OrchestrationBenchmark
    {
        protected override void ConfigureStorage(IServiceCollection services)
        {
            var connectionString = _configuration.GetConnectionString("Postgres");

            services.AddDurableTaskEFCoreStorage()
                .UseNpgsql(connectionString);
        }
    }
}
