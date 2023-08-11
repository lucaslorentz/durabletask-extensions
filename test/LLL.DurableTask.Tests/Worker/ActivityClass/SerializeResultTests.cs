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

namespace LLL.DurableTask.Tests.Worker.ActivityClass;

public class SerializeResultActivityMethodTests : WorkerTestBase
{
    public SerializeResultActivityMethodTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
    {
        base.ConfigureWorker(builder);

        builder.AddAnnotatedFrom(typeof(InvokeActivityOrchestration));
        builder.AddAnnotatedFrom(typeof(AsyncReturnGenericTaskString));
        builder.AddAnnotatedFrom(typeof(ReturnGenericTaskString));
    }

    [InlineData("AsyncReturnGenericTaskString", "\"Something\"")]
    [InlineData("ReturnGenericTaskString", "\"Something\"")]
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

    [Activity(Name = "AsyncReturnGenericTaskString")]
    public class AsyncReturnGenericTaskString : ActivityBase<object, string>
    {
        public override async Task<string> ExecuteAsync(object input)
        {
            await Task.Delay(1);
            return "Something";
        }
    }

    [Activity(Name = "ReturnGenericTaskString")]
    public class ReturnGenericTaskString : ActivityBase<object, string>
    {
        public override Task<string> ExecuteAsync(object input)
        {
            return Task.FromResult("Something");
        }
    }
}
