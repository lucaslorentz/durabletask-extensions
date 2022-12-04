# LLL.DurableTask.Api [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.Api)](https://www.nuget.org/packages/LLL.DurableTask.Api/)

Exposes orchestration management operations in a REST API.

## Depends on

- Storage
- Client

## Configuration

```C#
// Add Durable Task Api services
services.AddDurableTaskApi();
...
app.UseEndpoints(endpoints =>
{
    // Map Durable Task Api endpoints under /api prefix
    // Example of endpoint path: /api/v1/orchestrations
    endpoints.MapDurableTaskApi();
});
```

Alternatively you can define your own prefix:

```C#
app.UseEndpoints(endpoints =>
{
    // Map Durable Task Api endpoints under /tasks-api prefix
    // Example of endpoint path: /tasks-api/v1/orchestrations
    endpoints.MapDurableTaskApi("/tasks-api");
});
```

The API is integrated by default with [ASP.NET Core Authorization Policies](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-3.1). You must configure all Durable task policies and their requirements, like the example below:

```C#
services.AddAuthorization(c =>
{
    c.AddPolicy(DurableTaskPolicy.Entrypoint, p => p.RequireAssertion(x => true));
    c.AddPolicy(DurableTaskPolicy.Read, p => p.RequireRole("Reader"));
    c.AddPolicy(DurableTaskPolicy.ReadHistory, p => p.RequireRole("Reader"));
    c.AddPolicy(DurableTaskPolicy.Create, p => p.RequireRole("Administrator"));
    c.AddPolicy(DurableTaskPolicy.Terminate, p => p.RequireRole("Administrator"));
    c.AddPolicy(DurableTaskPolicy.RaiseEvent, p => p.RequireRole("Administrator"));
    c.AddPolicy(DurableTaskPolicy.Purge, p => p.RequireRole("Administrator"));
});
```

Alternatively, you can disable authorization integration on non production environments:

```C#
services.AddDurableTaskApi(options =>
{
    options.DisableAuthorization = true;
});
```

## Cross-Origin Requests (CORS)

CORS configuration is required if you run Durable Task API and Durable Task UI from different domains.

To configure CORS, please follow [Enable Cross-Origin Requests (CORS) in ASP.NET Core
](https://docs.microsoft.com/en-us/aspnet/core/security/cors).

Durable Task API requires http methods: **GET, POST, DELETE.**
