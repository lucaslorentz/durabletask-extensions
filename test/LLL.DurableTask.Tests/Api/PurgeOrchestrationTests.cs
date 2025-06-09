using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api;

public class PurgeOrchestrationTests : ApiTestBase
{
    public PurgeOrchestrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task PurgeOrchestration_ShouldReturnSuccess()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        var orchestrationInstance = await taskHubClient.CreateOrchestrationInstanceAsync("Test", "v1", null);

        var stateBeforePurge = await taskHubClient.GetOrchestrationStateAsync(orchestrationInstance.InstanceId);
        stateBeforePurge.Should().NotBeNull();

        using var httpClient = _host.GetTestClient();

        var httpResponse = await httpClient.DeleteAsync($"/api/v1/orchestrations/{orchestrationInstance.InstanceId}");
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var content = await httpResponse.Content.ReadAsStringAsync();
        content.Should().Be("{}");

        var stateAfterPurge = await taskHubClient.GetOrchestrationStateAsync(orchestrationInstance.InstanceId);
        stateAfterPurge.Should().BeNull();
    }
}
