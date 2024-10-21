var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddDurableTaskServerStorageGrpc(options =>
{
    options.BaseAddress = new Uri("http://localhost:5000");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

services.AddDurableTaskClient();

services.AddDurableTaskApi(options =>
{
    options.DisableAuthorization = true;
});

services.AddCors(c => c.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .WithMethods("GET", "POST", "DELETE")));

var app = builder.Build();

app.UseRouting();

app.UseCors();

app.MapDurableTaskApi();

app.Run();
