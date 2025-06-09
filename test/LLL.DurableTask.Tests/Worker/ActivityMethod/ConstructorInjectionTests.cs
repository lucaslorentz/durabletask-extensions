using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using LLL.DurableTask.Tests.Worker.TestHelpers;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker.ActivityMethod;

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

        builder.AddAnnotatedFrom(typeof(InvokeActivityOrchestration));
        builder.AddAnnotatedFrom(typeof(Activities));
    }

    [Fact]
    public async Task ActivityMethod_ShouldInjectConstructorDependencies()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        var instance = await taskHubClient.CreateOrchestrationInstanceAsync("InvokeActivity", "", null, new
        {
            Name = "Test"
        });

        var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

        result.Output.Should().Be("true");
    }

    public class SingletonClass { }
    public class ScopedClass { }
    public class TransientClass { }

    public class Activities
    {
        private readonly SingletonClass _singleton;
        private readonly ScopedClass _scoped;
        private readonly TransientClass _transient;

        public Activities(
            SingletonClass singleton,
            ScopedClass scoped,
            TransientClass transient)
        {
            _singleton = singleton;
            _scoped = scoped;
            _transient = transient;
        }

        [Activity(Name = "Test")]
        public Task<bool> Run()
        {
            return Task.FromResult(_singleton != null && _scoped != null && _transient != null);
        }
    }
}
