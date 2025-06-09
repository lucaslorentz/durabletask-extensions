using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using DurableTask.Core.History;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api;

public class RaiseOrchestrationEventTests : ApiTestBase
{
    public RaiseOrchestrationEventTests(ITestOutputHelper output) : base(output)
    {
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task RaiseOrchestrationEvent_ShouldReturnSuccess()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        var orchestrationInstance = await taskHubClient.CreateOrchestrationInstanceAsync("Test", "v1", null);

        using var httpClient = _host.GetTestClient();

        var eventData = new
        {
            field = "value"
        };
        var requestJson = JsonConvert.SerializeObject(eventData);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var httpResponse = await httpClient.PostAsync($"/api/v1/orchestrations/{orchestrationInstance.InstanceId}/raiseevent/TestEvent", requestContent);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var content = await httpResponse.Content.ReadAsStringAsync();
        content.Should().Be("{}");

        var messages = GetOrchestrationMessages(orchestrationInstance.InstanceId);
        messages.Should().HaveCount(2);

        var eventRaisedEvent = messages.Last().Event.Should().BeOfType<EventRaisedEvent>().Subject;
        eventRaisedEvent.Name.Should().Be("TestEvent");
        eventRaisedEvent.Input.Should().Be("{\"field\":\"value\"}");
    }
}
