using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class DurableTaskServerServiceCollectionExtensions
{
    public static IServiceCollection AddDurableTaskUi(
        this IServiceCollection services,
        Action<DurableTaskUiOptions> configure = null)
    {
        services.AddOptions<DurableTaskUiOptions>();

        if (configure != null)
            services.Configure<DurableTaskUiOptions>(configure);

        return services;
    }
}
