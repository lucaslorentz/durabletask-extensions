using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StoragesBenchmark;

public class SqlServerEFCoreBenchmark : OrchestrationBenchmark
{
    protected override void ConfigureStorage(IServiceCollection services)
    {
        var connectionString = _configuration.GetConnectionString("SqlServer");

        services.AddDurableTaskEFCoreStorage()
            .UseSqlServer(connectionString);
    }
}
