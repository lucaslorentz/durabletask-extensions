using System;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker.OrchestrationMethod
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
                .AddFromType(typeof(Orchestrations));
        }

        [InlineData("AsyncThrow")]
        [InlineData("SyncThrow")]
        [Theory]
        public async Task ThrowException_ShouldHaveFailureDetails(string orchestrationName)
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(orchestrationName, "", null);

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.OrchestrationStatus.Should().Be(OrchestrationStatus.Failed);
            result.FailureDetails.Should().NotBeNull();
            result.FailureDetails.ErrorMessage.Should().Be("SomeError");
        }

        public class Orchestrations
        {
            [Orchestration(Name = "AsyncThrow")]
            public async Task AsyncThrow(OrchestrationContext context)
            {
                await context.CreateTimer(context.CurrentUtcDateTime, 0);
                throw new Exception("SomeError");
            }

            [Orchestration(Name = "SyncThrow")]
            public void SyncThrow()
            {
                throw new Exception("SomeError");
            }
        }
    }
}