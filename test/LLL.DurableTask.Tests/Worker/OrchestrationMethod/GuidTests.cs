using System;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker.OrchestrationMethod
{
    public class GuidTests : WorkerTestBase
    {
        public GuidTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConfigureWorker(builder);

            builder.AddAnnotatedFrom(typeof(Orchestrations));
        }

        [Fact]
        public async Task GuidGenerator_ShouldBeAvailable()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance1 = await taskHubClient.CreateOrchestrationInstanceAsync("GenerateGuid", "", null);
            var instance2 = await taskHubClient.CreateOrchestrationInstanceAsync("GenerateGuid", "", null);

            var result1 = await taskHubClient.WaitForOrchestrationAsync(instance1, TimeSpan.FromSeconds(5));
            var result2 = await taskHubClient.WaitForOrchestrationAsync(instance2, TimeSpan.FromSeconds(5));

            result1.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
            result2.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
            result1.Output.Should().HaveLength(38);
            result2.Output.Should().HaveLength(38);
            result1.Output.Should().NotBe(result2.Output);
        }

        public static class Orchestrations
        {
            [Orchestration(Name = "GenerateGuid")]
            public static Guid GenerateGuid(ExtendedOrchestrationContext context)
            {
                return context.NewGuid();
            }
        }
    }
}