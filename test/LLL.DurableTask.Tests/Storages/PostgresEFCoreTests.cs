using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    [Collection("Postgres")]
    public class PostgresEFCoreTests : StorageTestBase
    {
        public PostgresEFCoreTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureStorage(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("Postgres");

            Skip.If(string.IsNullOrEmpty(connectionString), "Postgres connection string not configured");

            services.AddDurableTaskEFCoreStorage()
                .UseNpgsql(connectionString);
        }
    }
}