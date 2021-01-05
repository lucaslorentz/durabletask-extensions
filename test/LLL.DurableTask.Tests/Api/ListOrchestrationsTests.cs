using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api
{
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

            var firstPageHttpResponse = await httpClient.GetAsync("/api/v1/orchestrations?top=5");
            firstPageHttpResponse.StatusCode.Should().Be(200);
            firstPageHttpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var firstPageResponse = JsonConvert.DeserializeObject<OrchestrationQueryResult>(await firstPageHttpResponse.Content.ReadAsStringAsync());
            firstPageResponse.Count.Should().Be(7);
            firstPageResponse.ContinuationToken.Should().NotBeEmpty();
            firstPageResponse.Orchestrations.Should().HaveCount(5);
            firstPageResponse.Orchestrations[0].Name.Should().Be("Name-7");
            firstPageResponse.Orchestrations[0].Version.Should().Be("Version-7");
            firstPageResponse.Orchestrations[0].OrchestrationInstance.Should().NotBeNull();
            firstPageResponse.Orchestrations[0].OrchestrationInstance.InstanceId.Should().Be("InstanceId-7");
            firstPageResponse.Orchestrations[0].OrchestrationInstance.ExecutionId.Should().NotBeEmpty();
            firstPageResponse.Orchestrations[0].ParentInstance.Should().BeNull();
            firstPageResponse.Orchestrations[0].Input.Should().Be("\"Input-7\"");
            firstPageResponse.Orchestrations[0].Output.Should().BeNull();
            firstPageResponse.Orchestrations[0].OrchestrationStatus.Should().Be(OrchestrationStatus.Running);
            firstPageResponse.Orchestrations[0].Status.Should().BeNull();
            firstPageResponse.Orchestrations[0].CreatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, 5000);
            firstPageResponse.Orchestrations[0].LastUpdatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, 5000);
            firstPageResponse.Orchestrations[0].CompletedTime.Year.Should().Be(9999);

            firstPageResponse.Orchestrations[4].Name.Should().Be("Name-3");

            var secondPageHttpResponse = await httpClient.GetAsync($"/api/v1/orchestrations?top=5&continuationToken={firstPageResponse.ContinuationToken}");
            secondPageHttpResponse.StatusCode.Should().Be(200);
            secondPageHttpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var secondPageResponse = JsonConvert.DeserializeObject<OrchestrationQueryResult>(await secondPageHttpResponse.Content.ReadAsStringAsync());
            secondPageResponse.Count.Should().Be(7);
            secondPageResponse.ContinuationToken.Should().BeNull();
            secondPageResponse.Orchestrations.Should().HaveCount(2);
            secondPageResponse.Orchestrations[0].Name.Should().Be("Name-2");
            secondPageResponse.Orchestrations[1].Name.Should().Be("Name-1");
        }
    }
}