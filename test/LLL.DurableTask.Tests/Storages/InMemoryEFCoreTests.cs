using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    public class InMemoryEFCoreTests : EFCoreTestBase
    {
        public InMemoryEFCoreTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureStorage(IServiceCollection services)
        {
            services.AddDurableTaskEFCoreStorage()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());
        }
    }
}