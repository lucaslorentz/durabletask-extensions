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

namespace LLL.DurableTask.Tests.Worker.OrchestrationClass
{
    public class FailureDetailsTests : WorkerTestBase
    {
        public FailureDetailsTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConfigureWorker(builder);

            builder
                .AddFromType(typeof(ThrowOrchestration));
        }

        [Fact]
        public async Task ThrowException_ShouldHaveFailureDetails()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("Throw", "", null);

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.OrchestrationStatus.Should().Be(OrchestrationStatus.Failed);
            result.FailureDetails.Should().NotBeNull();
            result.FailureDetails.ErrorMessage.Should().Be("SomeError");
        }

        [Orchestration(Name = "Throw")]
        public class ThrowOrchestration : OrchestrationBase<string, object>
        {
            public override async Task<string> Execute(object input)
            {
                await Context.CreateTimer(Context.CurrentUtcDateTime, 0);
                throw new Exception("SomeError");
            }
        }
    }
}