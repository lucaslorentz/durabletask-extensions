using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages;

public class InMemoryDistributedTracingTests : DistributedTracingTestBase
{
    private readonly string _databaseId;

    public InMemoryDistributedTracingTests(ITestOutputHelper output) : base(output)
    {
        _databaseId = Guid.NewGuid().ToString();
    }

    protected override void ConfigureStorage(IServiceCollection services)
    {
        services.AddDurableTaskEFCoreStorage()
            .UseInMemoryDatabase(_databaseId);
    }
}
