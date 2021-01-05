using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Api.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api
{
    public class RewindOrchestrationTests : ApiTestBase
    {
        public RewindOrchestrationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Trait("Category", "Integration")]
        [Fact]
        public async Task RewindOrchestration_ShouldReturnSuccess()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var orchestrationInstance = await taskHubClient.CreateOrchestrationInstanceAsync("Test", "v1", null);

            using var httpClient = _host.GetTestClient();

            var request = new RewindRequest
            {
                Reason = "Some reason"
            };
            var requestJson = JsonConvert.SerializeObject(request);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync($"/api/v1/orchestrations/{orchestrationInstance.InstanceId}/rewind", requestContent);
            httpResponse.StatusCode.Should().Be(200);
            httpResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var content = await httpResponse.Content.ReadAsStringAsync();
            content.Should().Be("{}");
        }
    }
}