using System;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;
using LLL.DurableTask.EFCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Api
{
    public class ApiTestBase : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        protected IHost _host;
        protected IConfiguration Configuration { get; }

        public ApiTestBase(ITestOutputHelper output)
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
                .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        ConfigureStorage(services);

                        services.AddRouting();

                        services.AddDurableTaskClient();

                        services.AddDurableTaskApi(options =>
                        {
                            options.DisableAuthorization = true;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapDurableTaskApi();
                        });
                    })
                ).StartAsync();
        }

        protected void ConfigureStorage(IServiceCollection services)
        {
            services.AddDurableTaskEFCoreStorage()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());
        }

        public virtual async Task DisposeAsync()
        {
            await _host.StopAsync();
            await _host.WaitForShutdownAsync();
            _host.Dispose();
        }

        protected TaskMessage[] GetOrchestrationMessages(string instanceId)
        {
            var dbContextFactory = _host.Services.GetRequiredService<Func<OrchestrationDbContext>>();
            using (var dbContext = dbContextFactory())
            {
                var dataConverter = new UntypedJsonDataConverter();

                return dbContext.OrchestrationMessages
                    .OrderBy(m => m.AvailableAt)
                    .ThenBy(m => m.SequenceNumber)
                    .Select(m => dataConverter.Deserialize<TaskMessage>(m.Message))
                    .ToArray();
            }
        }
    }
}