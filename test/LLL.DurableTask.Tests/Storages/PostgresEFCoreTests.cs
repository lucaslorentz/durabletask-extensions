using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages;

[Collection("Postgres")]
public class PostgresEFCoreTests : EFCoreTestBase
{
    public PostgresEFCoreTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override void ConfigureStorage(IServiceCollection services)
    {
        var connectionString = Configuration.GetConnectionString("Postgres");

        Skip.If(string.IsNullOrWhiteSpace(connectionString), "Postgres connection string not configured");

        services.AddDurableTaskEFCoreStorage()
            .UseNpgsql(connectionString);
    }
}
