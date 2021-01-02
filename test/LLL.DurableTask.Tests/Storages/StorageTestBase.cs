using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Tests.Activities;
using LLL.DurableTask.Tests.Orchestrations;
using LLL.DurableTask.Worker.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    public abstract class StorageTestBase : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private IHost _host;
        protected IConfiguration Configuration { get; }
        protected TimeSpan FastWaitTimeout { get; set; } = TimeSpan.FromSeconds(20);
        protected TimeSpan SlowWaitTimeout { get; set; } = TimeSpan.FromSeconds(30);
        protected bool SupportsMultipleExecutionStorage { get; set; } = true;
        protected bool SupportsTags { get; set; } = true;

        public StorageTestBase(ITestOutputHelper output)
        {
            _output = output;

            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.private.json", true)
                .AddEnvironmentVariables()
                .Build();
        }

        public virtual async Task InitializeAsync()
        {
            _host = await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddFilter(l => l >= LogLevel.Warning).AddXUnit(_output);
                })
                .ConfigureServices(services =>
                {
                    ConfigureStorage(services);

                    services.AddDurableTaskClient();

                    services.AddDurableTaskWorker(builder =>
                    {
                        ConigureWorker(builder);
                    });
                }).StartAsync();
        }

        protected abstract void ConfigureStorage(IServiceCollection services);

        protected virtual void ConigureWorker(IDurableTaskWorkerBuilder builder)
        {
            builder.AddOrchestration<EmptyOrchestration>(EmptyOrchestration.Name, EmptyOrchestration.Version);
            builder.AddOrchestration<ContinueAsNewOrchestration>(ContinueAsNewOrchestration.Name, ContinueAsNewOrchestration.Version);
            builder.AddOrchestration<ContinueAsNewEmptyOrchestration>(ContinueAsNewEmptyOrchestration.Name, ContinueAsNewEmptyOrchestration.Version);
            builder.AddOrchestration<ParentOrchestration>(ParentOrchestration.Name, ParentOrchestration.Version);
            builder.AddOrchestration<ParallelTasksOrchestration>(ParallelTasksOrchestration.Name, ParallelTasksOrchestration.Version);
            builder.AddOrchestration<FibonacciRecursiveOrchestration>(FibonacciRecursiveOrchestration.Name, FibonacciRecursiveOrchestration.Version);
            builder.AddActivity<SumActivity>(SumActivity.Name, SumActivity.Version);
            builder.AddActivity<SubtractActivity>(SubtractActivity.Name, SubtractActivity.Version);
            builder.AddActivity<MeasuredDelayActivity>(MeasuredDelayActivity.Name, MeasuredDelayActivity.Version);
        }

        public virtual async Task DisposeAsync()
        {
            await _host.StopAsync();
            await _host.WaitForShutdownAsync();
            _host.Dispose();
        }

        [Trait("Category", "Integration")]
        [SkippableFact]
        public async Task EmptyOrchestration_ShouldComplete()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var input = Guid.NewGuid();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(EmptyOrchestration.Name, EmptyOrchestration.Version, input);

            var state = await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);

            state.Should().NotBeNull();
            state.Output.Should().Be($"\"{input}\"");
            state.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
        }

        [Trait("Category", "Integration")]
        [SkippableTheory]
        [InlineData(ContinueAsNewEmptyOrchestration.Name, ContinueAsNewEmptyOrchestration.Version)]
        [InlineData(ContinueAsNewOrchestration.Name, ContinueAsNewOrchestration.Version)]
        public async Task ContinueAsNewOrchestration_ShouldComplete(string name, string version)
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(name, version, 5);

            if (SupportsMultipleExecutionStorage)
            {
                var firstExecutionState = await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);
                firstExecutionState.Should().NotBeNull();
                firstExecutionState.Output.Should().Be("4");
                firstExecutionState.OrchestrationStatus.Should().Be(OrchestrationStatus.ContinuedAsNew);
            }

            var lastExecution = new OrchestrationInstance { InstanceId = instance.InstanceId };
            var lastExecutionState = await taskHubClient.WaitForOrchestrationAsync(lastExecution, FastWaitTimeout);
            lastExecutionState.Should().NotBeNull();
            lastExecutionState.Output.Should().Be("0");
            lastExecutionState.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
        }

        [Trait("Category", "Integration")]
        [SkippableFact]
        public async Task ParentOrchestration_ShouldComplete()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(ParentOrchestration.Name, ParentOrchestration.Version, 5);

            var state = await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);

            state.Should().NotBeNull();
            state.Output.Should().Be("5");
            state.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
        }

        [Trait("Category", "Integration")]
        [SkippableFact]
        public async Task ParallelTasksOrchestration_ShouldExecuteInParallel()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();
            var orchestrationService = _host.Services.GetRequiredService<IOrchestrationService>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(
                ParallelTasksOrchestration.Name,
                ParallelTasksOrchestration.Version,
                orchestrationService.MaxConcurrentTaskActivityWorkItems);

            var state = await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);

            state.Should().NotBeNull();
            state.Output.Should().Be(orchestrationService.MaxConcurrentTaskActivityWorkItems.ToString());
            state.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
        }

        [Trait("Category", "Integration")]
        [SkippableFact]
        public async Task FibonacciOrchestration_ShouldComplete()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(
                FibonacciRecursiveOrchestration.Name,
                FibonacciRecursiveOrchestration.Version,
                Guid.NewGuid().ToString(),
                2,
                new Dictionary<string, string>
                {
                    { "Tag1", "Value1" },
                    { "Tag2", "Value2" }
                });

            var state = await taskHubClient.WaitForOrchestrationAsync(instance, SlowWaitTimeout);

            state.Should().NotBeNull();
            state.Output.Should().Be("1");
            state.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);

            if (SupportsTags)
            {
                state.Tags.Should().BeEquivalentTo(new Dictionary<string, string> {
                    { "Tag1", "Value1" },
                    { "Tag2", "Value2" }
                });
            }
        }
    }
}
