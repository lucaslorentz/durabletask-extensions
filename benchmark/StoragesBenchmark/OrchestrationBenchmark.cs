using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using DurableTask.Core;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace StoragesBenchmark
{
    [SimpleJob(RunStrategy.Monitoring, targetCount: 5)]

    public abstract class OrchestrationBenchmark
    {
        [Params(1000)]
        public int NumberOfOrchestrations;

        [Params(5)]
        public int NumberOfActivities;

        protected IHost _host;
        protected IConfiguration _configuration;

        [GlobalSetup]
        public async Task Setup()
        {
            _configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", false)
                   .AddJsonFile("appsettings.private.json", true)
                   .AddEnvironmentVariables()
                   .Build();

            _host = await Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    ConfigureStorage(services);

                    services.AddDurableTaskClient();

                    services.AddDurableTaskWorker(builder =>
                    {
                        ConfigureWorker(builder);
                    });
                }).StartAsync();
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        protected abstract void ConfigureStorage(IServiceCollection services);

        protected virtual void ConfigureWorker(IDurableTaskWorkerBuilder builder)
        {
            builder.AddFromType(typeof(Orchestrations));
        }

        [Benchmark]
        public async Task RunOrchestrations()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instances = new List<OrchestrationInstance>();

            for (var i = 0; i < NumberOfOrchestrations; i++)
            {
                var instance = await taskHubClient.CreateOrchestrationInstanceAsync("RunActivities", "", NumberOfActivities);
                instances.Add(instance);
            }

            foreach (var instance in instances)
            {
                var state = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromMinutes(30));
                if (state == null || state.OrchestrationStatus != OrchestrationStatus.Completed)
                    throw new Exception("Orchestration did not complete");
            }
        }

        public class Orchestrations
        {
            [Orchestration(Name = "RunActivities")]
            public async Task Run(OrchestrationContext context, int numberOfActivities)
            {
                var tasks = Enumerable.Range(0, numberOfActivities).Select(e => context.ScheduleTask<Guid>("EmptyActivity", ""));
                await Task.WhenAll(tasks);
            }

            [Activity(Name = "EmptyActivity")]
            public Guid EmptyActivity()
            {
                return Guid.NewGuid();
            }
        }
    }
}
