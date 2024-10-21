var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddGrpc();

// var mysqlConnectionString = "server=localhost;database=durabletask;user=root;password=root";

services.AddDurableTaskEFCoreStorage()
    .UseInMemoryDatabase("Sample");
// .UseNpgsql("Server=localhost;Port=5432;Database=durabletask;User Id=postgres;Password=root");
// .UseMySql(mysqlConnectionString, ServerVersion.AutoDetect(mysqlConnectionString));
// .UseSqlServer("server=localhost;database=durabletask;user=sa;password=P1ssw0rd");

services.AddDurableTaskServer(builder =>
{
    builder.AddGrpcEndpoints();
});

var app = builder.Build();

app.UseRouting();

app.MapDurableTaskServerGrpcService();

app.Run();

