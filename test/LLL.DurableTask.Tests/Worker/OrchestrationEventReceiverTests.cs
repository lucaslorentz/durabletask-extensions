using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using LLL.DurableTask.Worker.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker
{
    public class OrchestrationEventReceiverTests : WorkerTestBase
    {
        public OrchestrationEventReceiverTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override void ConigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConigureWorker(builder);

            builder.AddFromType(typeof(TestOrchestration));
        }

        [Fact]
        public async Task EventReceiver_ShouldReceiveEvents()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("Test", "", null);

            await taskHubClient.RaiseEventAsync(instance, "EventA", new EventA { FieldA = "ValueA" });
            await taskHubClient.RaiseEventAsync(instance, "EventB", new EventB { FieldB = "ValueB" });

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.Output.Should().Be("{\"eventA\":{\"fieldA\":\"ValueA\"},\"eventB\":{\"fieldB\":\"ValueB\"}}");
        }

        public class EventA
        {
            public string FieldA { get; set; }
        }
        public class EventB
        {
            public string FieldB { get; set; }
        }

        public class TestOrchestration
        {
            [Orchestration(Name = "Test")]
            public async Task<object> Run(OrchestrationContext context, OrchestrationEventReceiver eventReceiver)
            {
                var eventA = await eventReceiver.WaitForEventAsync<EventA>("EventA");
                var eventB = await eventReceiver.WaitForEventAsync<EventB>("EventB");

                var cts = new CancellationTokenSource();
                var timeoutTask = context.CreateTimer<object>(context.CurrentUtcDateTime, null, cts.Token);
                var eventTask = eventReceiver.WaitForEventAsync<object>("EventC", cts.Token);
                if (eventTask == await Task.WhenAny(timeoutTask, eventTask))
                {
                    cts.Cancel();
                    throw new Exception("Shouldn't receive EventC");
                }

                var bothEvents = new
                {
                    EventA = eventA,
                    EventB = eventB
                };

                return bothEvents;
            }
        }
    }
}