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
            services.AddDurableTaskEFCoreStorage()
                .UseMySql("server=localhost;database=durabletask;user=root;password=root");
        }
    }
}