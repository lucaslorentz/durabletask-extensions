using System.Runtime.InteropServices;

var builder = Host.CreateApplicationBuilder();

var services = builder.Services;
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

var app = builder.Build();

app.Run();
