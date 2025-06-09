using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using DurableTask.Core.Query;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api;

public class ListOrchestrationsTests : ApiTestBase
{
    public ListOrchestrationsTests(ITestOutputHelper output) : base(output)
    {
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task ListOrchestrations_ShouldReturnOrchestrations()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        for (var i = 1; i <= 7; i++)
        {
            await taskHubClient.CreateOrchestrationInstanceAsync($"Name-{i}", $"Version-{i}", $"InstanceId-{i}", $"Input-{i}", new Dictionary<string, string>
            {
                [$"Tag-{i}"] = $"Value-{i}"
            });
        }

        using var httpClient = _host.GetTestClient();

        var firstPageHttpResponse = await httpClient.GetAsync("/api/v1/orchestrations?pageSize=5");
        firstPageHttpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstPageHttpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var firstPageResponse = JsonConvert.DeserializeObject<OrchestrationQueryResult>(await firstPageHttpResponse.Content.ReadAsStringAsync());
        firstPageResponse.ContinuationToken.Should().NotBeEmpty();
        firstPageResponse.OrchestrationState.Should().HaveCount(5);
        var firstOrchestration = firstPageResponse.OrchestrationState.First();
        firstOrchestration.Name.Should().Be("Name-7");
        firstOrchestration.Version.Should().Be("Version-7");
        firstOrchestration.OrchestrationInstance.Should().NotBeNull();
        firstOrchestration.OrchestrationInstance.InstanceId.Should().Be("InstanceId-7");
        firstOrchestration.OrchestrationInstance.ExecutionId.Should().NotBeEmpty();
        firstOrchestration.ParentInstance.Should().BeNull();
        firstOrchestration.Input.Should().Be("\"Input-7\"");
        firstOrchestration.Output.Should().BeNull();
        firstOrchestration.OrchestrationStatus.Should().Be(OrchestrationStatus.Running);
        firstOrchestration.Status.Should().BeNull();
        firstOrchestration.CreatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        firstOrchestration.LastUpdatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        firstOrchestration.CompletedTime.Year.Should().Be(9999);

        var fifthOrchestration = firstPageResponse.OrchestrationState.ElementAt(4);
        fifthOrchestration.Name.Should().Be("Name-3");

        var secondPageHttpResponse = await httpClient.GetAsync($"/api/v1/orchestrations?pageSize=5&continuationToken={firstPageResponse.ContinuationToken}");
        secondPageHttpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondPageHttpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var secondPageResponse = JsonConvert.DeserializeObject<OrchestrationQueryResult>(await secondPageHttpResponse.Content.ReadAsStringAsync());
        secondPageResponse.ContinuationToken.Should().BeNull();
        secondPageResponse.OrchestrationState.Should().HaveCount(2);
        secondPageResponse.OrchestrationState.ElementAt(0).Name.Should().Be("Name-2");
        secondPageResponse.OrchestrationState.ElementAt(1).Name.Should().Be("Name-1");
    }
}
