using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Net.Http;
using FlightWorker.Activities;

namespace FlightWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices);

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddDurableTaskServerStorageGrpc(options =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    options.BaseAddress = new Uri("http://localhost:5000");
                }
                else
                {
                    options.BaseAddress = new Uri("https://localhost:5001");
                }
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            services.AddDurableTaskWorker(builder =>
            {
                builder.AddActivity<BookFlightActivity>("BookFlight", "v1");
                builder.AddActivity<CancelFlightActivity>("CancelFlight", "v1");
            });
        }
    }
}
