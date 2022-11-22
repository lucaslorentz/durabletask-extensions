using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    public class InMemoryEFCoreServerTests : ServerStorageTestBase
    {
        private readonly string _databaseId;

        public InMemoryEFCoreServerTests(ITestOutputHelper output) : base(output)
        {
            _databaseId = Guid.NewGuid().ToString();
        }

        protected override void ConfigureServerStorage(IServiceCollection services)
        {
            services.AddDurableTaskEFCoreStorage()
                .UseInMemoryDatabase(_databaseId);
        }
    }
}