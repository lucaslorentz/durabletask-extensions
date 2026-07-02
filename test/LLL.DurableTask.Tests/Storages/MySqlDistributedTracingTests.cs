#if !NET10_0
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages;

public class MySqlDistributedTracingTests : DistributedTracingTestBase
{
    public MySqlDistributedTracingTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override void ConfigureStorage(IServiceCollection services)
    {
        var connectionString = Configuration.GetConnectionString("MySql");

        Skip.If(string.IsNullOrWhiteSpace(connectionString), "MySql connection string not configured");

        services.AddDurableTaskEFCoreStorage()
             .UseMySql(connectionString, MySqlServerVersion.AutoDetect(connectionString));
    }
}
#endif
