using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    [Collection("MySql")]
    public class MySqlEFCoreTests : StorageTestBase
    {
        public MySqlEFCoreTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureStorage(IServiceCollection services)
        {
            services.AddDurableTaskEFCoreStorage()
                .UseMySql("server=localhost;database=durabletask;user=root;password=root");
        }
    }
}