using System;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Tests.Worker.TestHelpers;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker.ActivityMethod
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
                .AddAnnotatedFromType(typeof(InvokeActivityOrchestration))
                .AddAnnotatedFromType(typeof(Activities));
        }

        [InlineData("AsyncThrow")]
        [InlineData("SyncThrow")]
        [Theory]
        public async Task ThrowException_ShouldHaveFailureDetails(string activityName)
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("InvokeActivity", "", null, new
            {
                Name = activityName
            });

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.OrchestrationStatus.Should().Be(OrchestrationStatus.Failed);
            result.FailureDetails.Should().NotBeNull();
            result.FailureDetails.ErrorMessage.Should().Match("Task '*' (#0) failed with an unhandled exception: SomeError");
        }

        public class Activities
        {
            [Activity(Name = "AsyncThrow")]
            public async Task AsyncThrow()
            {
                await Task.Delay(1);
                throw new Exception("SomeError");
            }

            [Activity(Name = "SyncThrow")]
            public void SyncThrow()
            {
                throw new Exception("SomeError");
            }
        }
    }
}