using System.Threading.Tasks;
using LLL.DurableTask.Tests.Storages;
using LLL.DurableTask.Tests.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    public abstract class ServerStorageTestBase : StorageTestBase
    {
        private readonly ITestOutputHelper _output;
        private IHost _serverHost;

        protected ServerStorageTestBase(ITestOutputHelper output)
            : base(output)
        {
            _output = output;
        }

        public override async Task InitializeAsync()
        {
            _serverHost = await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddFilter(l => l >= LogLevel.Error).AddXUnit(_output);
                })
                .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        ConfigureServerStorage(services);

                        services.AddGrpc();

                        services.AddDurableTaskServer(builder =>
                        {
                            builder.AddGrpcEndpoints();
                        });
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

            await base.InitializeAsync();
        }

        protected abstract void ConfigureServerStorage(IServiceCollection services);

        protected override void ConfigureStorage(IServiceCollection services)
        {
            var testServer = _serverHost.GetTestServer();
            var handler = testServer.CreateHandler();

            services.AddDurableTaskServerStorageGrpc(options =>
            {
                options.BaseAddress = testServer.BaseAddress;
            })
            .AddHttpMessageHandler(() => new ResponseVersionHandler())
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();

            await _serverHost.StopAsync();
            await _serverHost.WaitForShutdownAsync();
            _serverHost.Dispose();
        }
    }
}
