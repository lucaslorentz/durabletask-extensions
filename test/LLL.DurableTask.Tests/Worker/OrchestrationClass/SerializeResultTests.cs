using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker.OrchestrationClass;

public class SerializeResultTests : WorkerTestBase
{
    public SerializeResultTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
    {
        base.ConfigureWorker(builder);

        builder.AddAnnotatedFrom(typeof(AsyncReturnGenericTaskString));
        builder.AddAnnotatedFrom(typeof(ReturnGenericTaskString));
    }

    [InlineData("AsyncReturnGenericTaskString", "\"Something\"")]
    [InlineData("ReturnGenericTaskString", "\"Something\"")]
    [Theory]
    public async Task ReturnGenericTask_ShouldComplete(string orchestrationName, string expectedOutput)
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        var instance = await taskHubClient.CreateOrchestrationInstanceAsync(orchestrationName, "", null);

        var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

        result.Output.Should().Be(expectedOutput);
    }

    [Orchestration(Name = "AsyncReturnGenericTaskString")]
    public class AsyncReturnGenericTaskString : OrchestrationBase<string, object>
    {
        public override async Task<string> Execute(object input)
        {
            return await Context.CreateTimer<string>(Context.CurrentUtcDateTime, "Something");
        }
    }

    [Orchestration(Name = "ReturnGenericTaskString")]
    public class ReturnGenericTaskString : OrchestrationBase<string, object>
    {
        public override Task<string> Execute(object input)
        {
            return Task.FromResult("Something");
        }
    }
}
