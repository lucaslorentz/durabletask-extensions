using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    [Collection("SqlServer")]
    public class SqlServerEFCoreTests : StorageTestBase
    {
        public SqlServerEFCoreTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureStorage(IServiceCollection services)
        {
            services.AddDurableTaskEFCoreStorage()
                .UseSqlServer("server=localhost;database=durabletask;user=sa;password=P1ssw0rd");
        }
    }
}