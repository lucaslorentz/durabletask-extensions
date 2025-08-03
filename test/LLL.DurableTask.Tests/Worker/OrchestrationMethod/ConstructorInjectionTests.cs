using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker.OrchestrationMethod;

public class ConstructorInjectionTests : WorkerTestBase
{
    public ConstructorInjectionTests(ITestOutputHelper output)
        : base(output)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton<SingletonClass>();
        services.AddScoped<ScopedClass>();
        services.AddTransient<TransientClass>();
    }

    protected override void ConfigureWorker(IDurableTaskWorkerBuilder builder)
    {
        base.ConfigureWorker(builder);

        builder.AddAnnotatedFrom(typeof(Orchestrations));
    }

    [Fact]
    public async Task OrchestrationMethod_ShouldInjectConstructorDependencies()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        var instance = await taskHubClient.CreateOrchestrationInstanceAsync("Test", "", null);

        var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

        result.Output.Should().Be("true");
    }

    public class Orchestrations
    {
        private readonly SingletonClass _singleton;
        private readonly ScopedClass _scoped;
        private readonly TransientClass _transient;

        public Orchestrations(
            SingletonClass singleton,
            ScopedClass scoped,
            TransientClass transient)
        {
            _singleton = singleton;
            _scoped = scoped;
            _transient = transient;
        }

        [Orchestration(Name = "Test")]
        public Task<bool> Run()
        {
            return Task.FromResult(_singleton is not null && _scoped is not null && _transient is not null);
        }
    }
    public class SingletonClass { }
    public class ScopedClass { }
    public class TransientClass { }
}
