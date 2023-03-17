using System;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Tests.Worker.TestHelpers;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker.ActivityClass
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
                .AddAnnotatedFrom(typeof(InvokeActivityOrchestration))
                .AddAnnotatedFrom(typeof(ThrowActivity));
        }

        [Fact]
        public async Task ThrowException_ShouldHaveFailureDetails()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("InvokeActivity", "", new
            {
                Name = "Throw"
            });

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.OrchestrationStatus.Should().Be(OrchestrationStatus.Failed);
            result.FailureDetails.Should().NotBeNull();
            result.FailureDetails.ErrorMessage.Should().Match("Task '*' (#0) failed with an unhandled exception: SomeError");
        }

        [Activity(Name = "Throw")]
        public class ThrowActivity : ActivityBase<object, string>
        {
            public override Task<string> ExecuteAsync(object input)
            {
                throw new Exception("SomeError");
            }
        }
    }
}