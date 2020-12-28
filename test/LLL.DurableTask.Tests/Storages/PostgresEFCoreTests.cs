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
            services.AddDurableTaskEFCoreStorage()
                .UseNpgsql("Server=localhost;Port=5432;Database=durabletask;User Id=postgres;Password=root");
        }
    }
}