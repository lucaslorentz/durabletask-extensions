using System;
using System.Collections.Generic;
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
    public class HubSqlServerTests
    {
        private readonly ITestOutputHelper _output;

        public HubSqlServerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
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

                        services.AddDurableTaskServer(builder => {
                            builder.AddGrpcEndpoints();
                        });

                        services.AddDurableTaskEFCoreStorage()
                            .UseSqlServer("server=localhost;database=durabletask;user=sa;password=P1ssw0rd");
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapTaskHubServerGrpcEndpoints();
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
                    });
                }).StartAsync();

            var taskHubClient = clientHost.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(
                FibonacciRecursiveOrchestration.Name, FibonacciRecursiveOrchestration.Version,
                Guid.NewGuid().ToString(),
                new FibonacciRecursiveOrchestration.Input
                {
                    Number = 2
                },
                new Dictionary<string, string>
                {
                    { "Tag1", "Value1" },
                    { "Tag2", "Value2" }
                });

            var state = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30));

            try { await clientHost.StopAsync(); } catch (OperationCanceledException) { }
            await clientHost.WaitForShutdownAsync();
            try { await serverHost.StopAsync(); } catch (OperationCanceledException) { }
            await serverHost.WaitForShutdownAsync();
        }
    }
}
