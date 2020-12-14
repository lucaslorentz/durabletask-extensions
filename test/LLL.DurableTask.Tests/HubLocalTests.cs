using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Server.Tests.Activities;
using LLL.DurableTask.Server.Tests.Orchestrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Server.Tests
{
    [Trait("Category", "Integration")]
    public class HubLocalTests
    {
        private readonly ITestOutputHelper _output;

        public HubLocalTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Emulator implementation relies on TaskActivityWorkItem reference equality")]
        public async Task FibonacciOrchestration()
        {
            // Arrange
            using var serverHost = await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddFilter(l => l >= LogLevel.Error).AddXUnit(_output);
                })
                .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddGrpc();

                        services.AddDurableTaskServer(builder =>
                        {
                            builder.AddGrpcEndpoints();
                        });

                        services.AddDurableTaskEmulatorStorage();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapDurableTaskServerGrpcService();
                        });
                    })
                ).StartAsync();

            var handler = serverHost.GetTestServer().CreateHandler();

            using var clientHost = await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddFilter(l => l >= LogLevel.Error).AddXUnit(_output);
                })
                .ConfigureServices(services =>
                {
                    services.AddDurableTaskServerStorageGrpc(options =>
                    {
                        options.BaseAddress = serverHost.GetTestServer().BaseAddress;
                    })
                    .AddHttpMessageHandler(() => new ResponseVersionHandler())
                    .ConfigurePrimaryHttpMessageHandler(() => handler);

                    services.AddDurableTaskClient();

                    services.AddDurableTaskWorker(builder =>
                    {
                        builder.AddOrchestration<FibonacciRecursiveOrchestration>(FibonacciRecursiveOrchestration.Name, FibonacciRecursiveOrchestration.Version);
                        builder.AddActivity<SumActivity>(SumActivity.Name, SumActivity.Version);
                        builder.HasAllOrchestrations = true;
                        builder.HasAllActivities = true;
                    });
                }).StartAsync();

            var taskHubClient = clientHost.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(FibonacciRecursiveOrchestration.Name, FibonacciRecursiveOrchestration.Version, new FibonacciRecursiveOrchestration.Input
            {
                Number = 2
            });

            var state = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30));
        }
    }
}
