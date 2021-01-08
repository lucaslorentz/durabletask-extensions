using System;
using System.Threading.Tasks;
using LLL.DurableTask.Worker.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Worker
{
    public abstract class WorkerTestBase : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        protected IHost _host;
        protected IConfiguration Configuration { get; }
        protected TimeSpan FastWaitTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public WorkerTestBase(ITestOutputHelper output)
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

                    ConfigureServices(services);

                    services.AddDurableTaskClient();

                    services.AddDurableTaskWorker(builder =>
                    {
                        ConigureWorker(builder);
                    });
                }).StartAsync();
        }

        private void ConfigureStorage(IServiceCollection services)
        {
            services.AddDurableTaskEFCoreStorage()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
        }

        protected virtual void ConigureWorker(IDurableTaskWorkerBuilder builder)
        {
        }

        public virtual async Task DisposeAsync()
        {
            await _host.StopAsync();
            await _host.WaitForShutdownAsync();
            _host.Dispose();
        }
    }
}
