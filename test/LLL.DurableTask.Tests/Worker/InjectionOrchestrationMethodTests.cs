using System;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.Worker.Attributes;
using LLL.DurableTask.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker
{
    public class InjectionOrchestrationMethodTests : WorkerTestBase
    {
        public InjectionOrchestrationMethodTests(ITestOutputHelper output)
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

        protected override void ConigureWorker(IDurableTaskWorkerBuilder builder)
        {
            base.ConigureWorker(builder);

            builder.AddFromType(typeof(TestOrchestration));
        }

        [Fact]
        public async Task MethodOrchestrationConstructorDependencies_ShouldHaveDependenciesInjectedAsync()
        {
            var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync("Test", "", null);

            var result = await taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(5));

            result.Output.Should().Be("true");
        }

        public class SingletonClass { }
        public class ScopedClass { }
        public class TransientClass { }

        public class TestOrchestration
        {
            private readonly SingletonClass _singleton;
            private readonly ScopedClass _scoped;
            private readonly TransientClass _transient;

            public TestOrchestration(
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
                return Task.FromResult(_singleton != null && _scoped != null && _transient != null);
            }
        }
    }
}