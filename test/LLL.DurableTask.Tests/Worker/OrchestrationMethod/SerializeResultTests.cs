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
    public class SerializeResultTests : WorkerTestBase
    {
        public SerializeResultTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConfigureWorker(builder);

            builder.AddAnnotatedFromType(typeof(Orchestrations));
        }

        [InlineData("AsyncReturnGenericTaskString", "\"Something\"")]
        [InlineData("ReturnGenericTaskString", "\"Something\"")]
        [InlineData("AsyncReturnTask", null)]
        [InlineData("ReturnTask", null)]
        [InlineData("ReturnString", "\"Something\"")]
        [InlineData("ReturnVoid", null)]
        [Theory]
        public async Task ReturnGenericTask_ShouldComplete(string orchestrationName, string expectedOutput)
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(orchestrationName, "", null);

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.Output.Should().Be(expectedOutput);
        }

        public class Orchestrations
        {
            [Orchestration(Name = "AsyncReturnGenericTaskString")]
            public async Task<string> AsyncReturnGenericTaskString(OrchestrationContext context)
            {
                return await context.CreateTimer<string>(context.CurrentUtcDateTime, "Something");
            }

            [Orchestration(Name = "ReturnGenericTaskString")]
            public Task<string> ReturnGenericTaskString()
            {
                return Task.FromResult("Something");
            }

            [Orchestration(Name = "AsyncReturnTask")]
            public async Task AsyncReturnTask(OrchestrationContext context)
            {
                await context.CreateTimer<object>(context.CurrentUtcDateTime, null);
            }

            [Orchestration(Name = "ReturnTask")]
            public Task ReturnTask()
            {
                return Task.CompletedTask;
            }

            [Orchestration(Name = "ReturnString")]
            public string ReturnString()
            {
                return "Something";
            }

            [Orchestration(Name = "ReturnVoid")]
            public void ReturnVoid()
            {
            }
        }
    }
}