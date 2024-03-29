﻿using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlightWorker;

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
            builder.AddAnnotatedFrom(typeof(Program).Assembly);
        });
    }
}
