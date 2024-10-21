var builder = WebApplication.CreateBuilder();

var services = builder.Services;
services.AddDurableTaskUi(options =>
{
    options.ApiBaseUrl = "https://localhost:5003/api";
});

var app = builder.Build();

app.UseDurableTaskUi();

app.Run();
