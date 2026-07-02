using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using DurableTask.Core.Settings;
using LLL.DurableTask.Tests.Storage.Activities;
using LLL.DurableTask.Tests.Storage.Orchestrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages;

// Distributed-tracing acceptance test. Runs single-threaded in its own collection because
// DurableTask.Core.Settings.CorrelationSettings.Current is a process-wide static. A dedicated
// base (rather than StorageTestBase) is required so tracing can be enabled before the host
// starts and only the orchestration/activity under test are registered.
[Collection("DistributedTracing")]
public abstract class DistributedTracingTestBase : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    protected IHost _host;
    protected IConfiguration Configuration { get; }
    protected TimeSpan FastWaitTimeout { get; set; } = TimeSpan.FromSeconds(60);

    protected DistributedTracingTestBase(ITestOutputHelper output)
    {
        _output = output;

#if NET10_0
        var framework = "net10";
#elif NET9_0
        var framework = "net9";
#elif NET8_0
        var framework = "net8";
#endif

        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{framework}.json", false)
            .AddEnvironmentVariables()
            .Build();
    }

    protected abstract void ConfigureStorage(IServiceCollection services);

    public virtual async Task InitializeAsync()
    {
        CorrelationSettings.Current.EnableDistributedTracing = true;
        CorrelationSettings.Current.Protocol = Protocol.W3CTraceContext;

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
                    builder.AddOrchestration<SingleActivityOrchestration>(SingleActivityOrchestration.Name, SingleActivityOrchestration.Version);
                    builder.AddActivity<SumActivity>(SumActivity.Name, SumActivity.Version);
                });
            }).StartAsync();
    }

    public virtual async Task DisposeAsync()
    {
        CorrelationSettings.Current.EnableDistributedTracing = false;
        await _host.StopAsync();
        await _host.WaitForShutdownAsync();
        _host.Dispose();
    }

    [Trait("Category", "Integration")]
    [SkippableFact]
    public async Task Tracing_ClientOrchestrationActivitySpans_ShouldConnect()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DurableTask.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => { lock (spans) { spans.Add(a); } },
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(listener);

        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        string rootTraceId;
        OrchestrationInstance instance;
        using (var root = new Activity("test-root").Start())
        {
            rootTraceId = root.TraceId.ToHexString();
            instance = await taskHubClient.CreateOrchestrationInstanceAsync(
                SingleActivityOrchestration.Name,
                SingleActivityOrchestration.Version,
                41);
        }

        // (a) Execution must complete quickly with tracing on.
        var state = await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);
        state.Should().NotBeNull("orchestration must complete when distributed tracing is enabled");
        state.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
        state.Output.Should().Be("42");

        // Give any late spans a moment to flush.
        await Task.Delay(200);

        List<Activity> captured;
        lock (spans) { captured = spans.ToList(); }

        var names = captured.Select(s => $"{s.DisplayName} trace={s.TraceId.ToHexString()}").ToList();
        _output.WriteLine($"root trace id: {rootTraceId}");
        _output.WriteLine("captured spans:\n  " + string.Join("\n  ", names));

        var create = captured.FirstOrDefault(s => s.DisplayName.StartsWith("create_orchestration"));
        var orch = captured.FirstOrDefault(s => s.DisplayName.StartsWith("orchestration"));
        var activity = captured.FirstOrDefault(s => s.DisplayName.StartsWith("activity"));

        // (b) All three spans exist and share the caller's root trace id.
        create.Should().NotBeNull("client should emit create_orchestration span");
        create.TraceId.ToHexString().Should().Be(rootTraceId);

        orch.Should().NotBeNull("worker should emit orchestration span");
        orch.TraceId.ToHexString().Should().Be(rootTraceId, "orchestration span must connect to caller root");

        activity.Should().NotBeNull("worker should emit activity span");
        activity.TraceId.ToHexString().Should().Be(rootTraceId, "activity span must connect to caller root");
    }
}

[CollectionDefinition("DistributedTracing", DisableParallelization = true)]
public class DistributedTracingCollection { }
