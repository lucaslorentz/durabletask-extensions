using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Server
{
    [Collection("MySql")]
    public class MySqlEFCoreServerTests : ServerTestsBase
    {
        public MySqlEFCoreServerTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureServerStorage(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("MySql");

            Skip.If(string.IsNullOrWhiteSpace(connectionString), "MySql connection string not configured");

            services.AddDurableTaskEFCoreStorage()
                .UseMySql(connectionString);
        }
    }
}