using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using FluentAssertions;
using LLL.DurableTask.Api.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api
{
    public class TerminateOrchestrationTests : ApiTestBase
    {
        public TerminateOrchestrationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Trait("Category", "Integration")]
        [Fact]
        public async Task TerminateOrchestration_ShouldReturnSuccess()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var orchestrationInstance = await taskHubClient.CreateOrchestrationInstanceAsync("Test", "v1", null);

            using var httpClient = _host.GetTestClient();

            var request = new TerminateRequest
            {
                Reason = "Some reason"
            };
            var requestJson = JsonConvert.SerializeObject(request);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync($"/api/v1/orchestrations/{orchestrationInstance.InstanceId}/terminate", requestContent);
            httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var content = await httpResponse.Content.ReadAsStringAsync();
            content.Should().Be("{}");

            var messages = GetOrchestrationMessages(orchestrationInstance.InstanceId);
            messages.Should().HaveCount(2);

            var terminatedEvent = messages.Last().Event.Should().BeOfType<ExecutionTerminatedEvent>().Subject;
            terminatedEvent.Input.Should().Be("Some reason");
        }
    }
}