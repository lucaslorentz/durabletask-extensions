using System;
using System.Collections.Generic;
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
    public class EventReceiverTests : WorkerTestBase
    {
        public EventReceiverTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override void ConigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConigureWorker(builder);

            builder.AddFromType(typeof(WaitForEventOrchestration));
            builder.AddFromType(typeof(AddEventListenerOrchestration));
        }

        [Fact]
        public async Task WaitForEventAsync_ShouldReceiveEvents()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("WaitForEvent", "", null);

            await taskHubClient.RaiseEventAsync(instance, "EventA", new EventA { FieldA = "A" });
            await taskHubClient.RaiseEventAsync(instance, "EventB", new EventB { FieldB = "B" });

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.Output.Should().Be("{\"eventA\":{\"fieldA\":\"A\"},\"eventB\":{\"fieldB\":\"B\"}}");
        }

        [Fact]
        public async Task AddEventListener_ShouldReceiveEvents()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("AddEventListener", "", null);

            await taskHubClient.RaiseEventAsync(instance, "EventA", new EventA { FieldA = "1" });
            await taskHubClient.RaiseEventAsync(instance, "EventB", new EventB { FieldB = "1" });
            await taskHubClient.RaiseEventAsync(instance, "EventA", new EventA { FieldA = "2" });
            await taskHubClient.RaiseEventAsync(instance, "EventB", new EventB { FieldB = "2" });
            await taskHubClient.RaiseEventAsync(instance, "Stop", null);

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.Output.Should().Be("{\"eventsA\":[{\"fieldA\":\"1\"},{\"fieldA\":\"2\"}],\"eventsB\":[{\"fieldB\":\"1\"},{\"fieldB\":\"2\"}]}");
        }

        [Orchestration(Name = "WaitForEvent")]
        public class WaitForEventOrchestration : OrchestrationBase<object, object>
        {
            public override async Task<object> Execute(object input)
            {
                var eventA = await EventReceiver.WaitForEventAsync<EventA>("EventA");
                var eventB = await EventReceiver.WaitForEventAsync<EventB>("EventB");
                var eventC = await EventReceiver.WaitForEventAsync<object>("EventC", TimeSpan.FromSeconds(1), null);

                var result = new
                {
                    EventA = eventA,
                    EventB = eventB,
                    EventC = eventC
                };

                return result;
            }
        }

        [Orchestration(Name = "AddEventListener")]
        public class AddEventListenerOrchestration : OrchestrationBase<object, object>
        {
            public override async Task<object> Execute(object input)
            {
                List<EventA> eventsA = new List<EventA>();
                List<EventB> eventsB = new List<EventB>();

                using (EventReceiver.AddListener<EventA>("EventA", eventsA.Add))
                using (EventReceiver.AddListener<EventB>("EventB", eventsB.Add))
                {
                    await EventReceiver.WaitForEventAsync<object>("Stop");
                }

                var result = new
                {
                    EventsA = eventsA,
                    EventsB = eventsB
                };

                return result;
            }
        }
        public class EventA
        {
            public string FieldA { get; set; }
        }
        public class EventB
        {
            public string FieldB { get; set; }
        }
    }
}