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
    public class SerializeResultActivityMethodTests : WorkerTestBase
    {
        public SerializeResultActivityMethodTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConfigureWorker(builder);

            builder.AddAnnotatedFrom(typeof(InvokeActivityOrchestration));
            builder.AddAnnotatedFrom(typeof(Activities));
        }

        [InlineData("AsyncReturnGenericTaskString", "\"Something\"")]
        [InlineData("ReturnGenericTaskString", "\"Something\"")]
        [InlineData("AsyncReturnTask", null)]
        [InlineData("ReturnTask", null)]
        [InlineData("ReturnString", "\"Something\"")]
        [InlineData("ReturnVoid", null)]
        [Theory]
        public async Task ReturnGenericTask_ShouldComplete(string activityName, string expectedOutput)
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("InvokeActivity", "", null, new
            {
                Name = activityName
            });

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.Output.Should().Be(expectedOutput);
        }

        public class Activities
        {
            [Activity(Name = "AsyncReturnGenericTaskString")]
            public async Task<string> AsyncReturnGenericTaskString()
            {
                await Task.Delay(1);
                return "Something";
            }

            [Activity(Name = "ReturnGenericTaskString")]
            public Task<string> ReturnGenericTaskString()
            {
                return Task.FromResult("Something");
            }

            [Activity(Name = "AsyncReturnTask")]
            public async Task AsyncReturnTask()
            {
                await Task.Delay(1);
            }

            [Activity(Name = "ReturnTask")]
            public Task ReturnTask()
            {
                return Task.CompletedTask;
            }

            [Activity(Name = "ReturnString")]
            public string ReturnString()
            {
                return "Something";
            }

            [Activity(Name = "ReturnVoid")]
            public void ReturnVoid()
            {
            }
        }
    }
}