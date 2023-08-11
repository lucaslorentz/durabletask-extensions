using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using FluentAssertions;
using LLL.DurableTask.Server.Api.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api;

public class CreateOrchestrationTests : ApiTestBase
{
    public CreateOrchestrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task CreateOrchestration_ShouldValidateRequest()
    {
        using var httpClient = _host.GetTestClient();

        var request = new CreateOrchestrationRequest();
        var requestJson = JsonConvert.SerializeObject(request);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var httpResponse = await httpClient.PostAsync("/api/v1/orchestrations", requestContent);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var response = await httpResponse.Content.ReadAsStringAsync();
        response.Should().Be("[{\"memberNames\":[\"Name\"],\"errorMessage\":\"The Name field is required.\"}]");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task CreateOrchestration_ShouldCreateOrchestration()
    {
        using var httpClient = _host.GetTestClient();

        var request = new CreateOrchestrationRequest
        {
            Name = "SomeName",
            Version = "SomeVersion",
            InstanceId = "SomeInstanceId",
            Input = JObject.FromObject(new { key = "value" }),
            Tags = new Dictionary<string, string>
            {
                ["Tag"] = "Value"
            }
        };
        var requestJson = JsonConvert.SerializeObject(request);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var httpResponse = await httpClient.PostAsync("/api/v1/orchestrations", requestContent);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var orchestrationInstance = JsonConvert.DeserializeObject<OrchestrationInstance>(await httpResponse.Content.ReadAsStringAsync());
        orchestrationInstance.InstanceId.Should().Be("SomeInstanceId");
        orchestrationInstance.ExecutionId.Should().NotBeNullOrEmpty();

        httpResponse.Headers.Location.Should().Be($"http://localhost/api/v1/orchestrations/{orchestrationInstance.InstanceId}");

        var messages = GetOrchestrationMessages(orchestrationInstance.InstanceId);
        messages.Should().HaveCount(1);

        var startedEvent = messages.Last().Event.Should().BeOfType<ExecutionStartedEvent>().Subject;
        startedEvent.OrchestrationInstance.Should().BeEquivalentTo(orchestrationInstance);
        startedEvent.Name.Should().Be("SomeName");
        startedEvent.Version.Should().Be("SomeVersion");
        startedEvent.Input.Should().Be("{\"key\":\"value\"}");
        startedEvent.Tags.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["Tag"] = "Value"
        });

        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();
        var state = await taskHubClient.GetOrchestrationStateAsync(orchestrationInstance.InstanceId);
        state.Should().NotBeNull();
        state.OrchestrationInstance.Should().BeEquivalentTo(orchestrationInstance);
        state.Name.Should().Be("SomeName");
        state.Version.Should().Be("SomeVersion");
        state.Input.Should().Be("{\"key\":\"value\"}");
        state.Tags.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["Tag"] = "Value"
        });
    }
}
