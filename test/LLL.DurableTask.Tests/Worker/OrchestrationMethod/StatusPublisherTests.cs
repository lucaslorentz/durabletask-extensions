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
    public class StatusPublisherTests : WorkerTestBase
    {
        public StatusPublisherTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConfigureWorker(builder);

            builder.AddAnnotatedFrom(typeof(Orchestrations));
        }

        [Fact]
        public async Task PublishStatus_ShouldPublishStatus()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("PublishStatus", "", null);

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.Status.Should().Be("{\"message\":\"Status B\"}");
        }

        public static class Orchestrations
        {
            [Orchestration(Name = "PublishStatus")]
            public static async Task PublishStatus(OrchestrationContext context, OrchestrationStatusPublisher statusPublisher)
            {
                statusPublisher.Status = new Status { Message = "Status A" };
                var statusB = await context.ScheduleTask<Status>("GetStatusB", string.Empty);
                statusPublisher.Status = statusB;
            }

            [Activity(Name = "GetStatusB")]
            public static Status GetStatusB()
            {
                return new Status { Message = "Status B" };
            }

            public class Status
            {
                public string Message { get; set; }
            }
        }
    }
}