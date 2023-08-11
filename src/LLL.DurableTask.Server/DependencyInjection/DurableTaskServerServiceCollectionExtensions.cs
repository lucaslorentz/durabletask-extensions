using System;
using LLL.DurableTask.Server.Configuration;
using LLL.DurableTask.Server.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DurableTaskServerServiceCollectionExtensions
{
    public static IServiceCollection AddDurableTaskServer(
        this IServiceCollection services,
        Action<TaskHubServerBuilder> config = null)
    {
        services.AddHostedService<ServerHostedService>();

        var builder = new TaskHubServerBuilder(services);

        config?.Invoke(builder);

        return services;
    }
}
