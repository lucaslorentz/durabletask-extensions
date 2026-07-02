using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages;

public class SqlServerDistributedTracingTests : DistributedTracingTestBase
{
    public SqlServerDistributedTracingTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override void ConfigureStorage(IServiceCollection services)
    {
        var connectionString = Configuration.GetConnectionString("SqlServer");

        Skip.If(string.IsNullOrWhiteSpace(connectionString), "SqlServer connection string not configured");

        services.AddDurableTaskEFCoreStorage()
            .UseSqlServer(connectionString);
    }
}
