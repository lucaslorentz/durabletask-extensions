using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    public class EmulatorServerTests : StorageTestBase
    {
        public EmulatorServerTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureStorage(IServiceCollection services)
        {
            services.AddDurableTaskEmulatorStorage();
        }

        protected override void ConigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConigureWorker(builder);

            builder.HasAllOrchestrations = true;
            builder.HasAllActivities = true;
        }
    }
}