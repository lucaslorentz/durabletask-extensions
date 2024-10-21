using System.Runtime.InteropServices;
using BpmnWorker.Activities;
using BpmnWorker.Providers;
using BpmnWorker.Scripting;

var builder = Host.CreateApplicationBuilder(args);

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

services.AddSingleton<IBPMNProvider, LocalFileBPMNProvider>();

services.AddSingleton<CSharpScriptEngine>();
services.AddScoped<IScriptEngineFactory>(p => new ServiceProviderScriptEngineFactory<CSharpScriptEngine>(p, "c#"));
services.AddSingleton<JavaScriptScriptEngine>();
services.AddScoped<IScriptEngineFactory>(p => new ServiceProviderScriptEngineFactory<JavaScriptScriptEngine>(p, "javascript"));

services.AddScoped<IScriptExecutor, ScriptExecutor>();

var app = builder.Build();

app.Run();
