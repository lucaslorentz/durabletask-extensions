using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api
{
    public class GetOrchestrationTests : ApiTestBase
    {
        public GetOrchestrationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Trait("Category", "Integration")]
        [Fact]
        public async Task GetOrchestration_ShouldReturnOrchestration()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var orchestrationInstance = await taskHubClient.CreateOrchestrationInstanceAsync(
                "SomeName",
                "SomeVersion",
                "SomeInstanceId",
                JObject.FromObject(new { key = "value" }),
                new Dictionary<string, string>
                {
                    ["Tag"] = "Value"
                }
            );

            using var httpClient = _host.GetTestClient();

            var httpResponse = await httpClient.GetAsync($"/api/v1/orchestrations/{orchestrationInstance.InstanceId}");
            httpResponse.StatusCode.Should().Be(200);
            httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var state = JsonConvert.DeserializeObject<OrchestrationState>(await httpResponse.Content.ReadAsStringAsync());
            state.Should().NotBeNull();
            state.OrchestrationInstance.Should().BeEquivalentTo(orchestrationInstance);
            state.Name.Should().Be("SomeName");
            state.Version.Should().Be("SomeVersion");
            state.Input.Should().Be("{\"key\":\"value\"}");
            state.Tags.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["Tag"] = "Value"
            });
            state.ParentInstance.Should().BeNull();
            state.Output.Should().BeNull();
            state.OrchestrationStatus.Should().Be(OrchestrationStatus.Running);
            state.Status.Should().BeNull();
            state.CreatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, 5000);
            state.LastUpdatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, 5000);
            state.CompletedTime.Year.Should().Be(9999);
        }

        [Trait("Category", "Integration")]
        [Fact]
        public async Task GetOrchestrationExecution_ShouldReturnOrchestration()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var orchestrationInstance = await taskHubClient.CreateOrchestrationInstanceAsync(
                "SomeName",
                "SomeVersion",
                "SomeInstanceId",
                JObject.FromObject(new { key = "value" }),
                new Dictionary<string, string>
                {
                    ["Tag"] = "Value"
                }
            );

            using var httpClient = _host.GetTestClient();

            var httpResponse = await httpClient.GetAsync($"/api/v1/orchestrations/{orchestrationInstance.InstanceId}/{orchestrationInstance.ExecutionId}");
            httpResponse.StatusCode.Should().Be(200);
            httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var state = JsonConvert.DeserializeObject<OrchestrationState>(await httpResponse.Content.ReadAsStringAsync());
            state.Should().NotBeNull();
            state.OrchestrationInstance.Should().BeEquivalentTo(orchestrationInstance);
            state.Name.Should().Be("SomeName");
            state.Version.Should().Be("SomeVersion");
            state.Input.Should().Be("{\"key\":\"value\"}");
            state.Tags.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["Tag"] = "Value"
            });
            state.ParentInstance.Should().BeNull();
            state.Output.Should().BeNull();
            state.OrchestrationStatus.Should().Be(OrchestrationStatus.Running);
            state.Status.Should().BeNull();
            state.CreatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, 5000);
            state.LastUpdatedTime.Should().BeBefore(DateTime.UtcNow).And.BeCloseTo(DateTime.UtcNow, 5000);
            state.CompletedTime.Year.Should().Be(9999);
        }
    }
}