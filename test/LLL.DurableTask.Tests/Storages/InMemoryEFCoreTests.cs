using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages;

public class InMemoryEFCoreTests : EFCoreTestBase
{
    private readonly string _databaseId;

    public InMemoryEFCoreTests(ITestOutputHelper output) : base(output)
    {
        _databaseId = Guid.NewGuid().ToString();
    }

    protected override void ConfigureStorage(IServiceCollection services)
    {
        services.AddDurableTaskEFCoreStorage()
            .UseInMemoryDatabase(_databaseId);
    }
}
