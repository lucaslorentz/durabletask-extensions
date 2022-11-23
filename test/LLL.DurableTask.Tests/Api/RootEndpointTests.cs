using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using LLL.DurableTask.Api.Models;
using LLL.DurableTask.Core;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api
{
    public class RootEndpointTests : ApiTestBase
    {
        public RootEndpointTests(ITestOutputHelper output) : base(output)
        {
        }

        [Trait("Category", "Integration")]
        [Fact]
        public async Task RootEndpoint_ShouldReturnApiMetadata()
        {
            using var httpClient = _host.GetTestClient();

            var rootResponse = await httpClient.GetAsync("/api");
            rootResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            rootResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var response = JsonConvert.DeserializeObject<EntrypointResponse>(await rootResponse.Content.ReadAsStringAsync());

            response.Should().BeEquivalentTo(new EntrypointResponse
            {
                Endpoints = new Dictionary<string, EndpointInfo>
                {
                    ["Entrypoint"] = new EndpointInfo
                    {
                        Href = "/api",
                        Method = "GET",
                        Authorized = true
                    },
                    ["OrchestrationsList"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations",
                        Method = "GET",
                        Authorized = true
                    },
                    ["OrchestrationsCreate"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations",
                        Method = "POST",
                        Authorized = true
                    },
                    ["OrchestrationsGet"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations/{instanceId}",
                        Method = "GET",
                        Authorized = true
                    },
                    ["OrchestrationsGetExecution"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations/{instanceId}/{executionId}",
                        Method = "GET",
                        Authorized = true
                    },
                    ["OrchestrationsGetExecutionHistory"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations/{instanceId}/{executionId}/history",
                        Method = "GET",
                        Authorized = true
                    },
                    ["OrchestrationsTerminate"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations/{instanceId}/terminate",
                        Method = "POST",
                        Authorized = true
                    },
                    ["OrchestrationsRewind"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations/{instanceId}/rewind",
                        Method = "POST",
                        Authorized = true
                    },
                    ["OrchestrationsRaiseEvent"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations/{instanceId}/raiseevent/{eventName}",
                        Method = "POST",
                        Authorized = true
                    },
                    ["OrchestrationsPurgeInstance"] = new EndpointInfo
                    {
                        Href = "/api/v1/orchestrations/{instanceId}",
                        Method = "DELETE",
                        Authorized = true
                    }
                },
                Features = new OrchestrationFeature[]
                 {
                     OrchestrationFeature.SearchByInstanceId,
                     OrchestrationFeature.SearchByName,
                     OrchestrationFeature.SearchByCreatedTime,
                     OrchestrationFeature.SearchByStatus,
                     OrchestrationFeature.Rewind,
                     OrchestrationFeature.Tags,
                     OrchestrationFeature.StatePerExecution
                 }
            });
        }
    }
}