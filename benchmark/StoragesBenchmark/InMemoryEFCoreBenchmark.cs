using System;
using Microsoft.Extensions.DependencyInjection;

namespace StoragesBenchmark;

public class InMemoryEFCoreBenchmark : OrchestrationBenchmark
{
    protected override void ConfigureStorage(IServiceCollection services)
    {
        services.AddDurableTaskEFCoreStorage()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
    }
}
