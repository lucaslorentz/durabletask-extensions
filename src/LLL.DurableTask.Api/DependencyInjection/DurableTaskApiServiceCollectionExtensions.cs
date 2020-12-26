using System;
using LLL.DurableTask.Api.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DurableTaskApiServiceCollectionExtensions
    {
        public static IServiceCollection AddDurableTaskApi(
            this IServiceCollection services,
            Action<DurableTaskApiOptions> configure = null)
        {
            services.AddOptions<DurableTaskApiOptions>();

            if (configure != null)
                services.Configure<DurableTaskApiOptions>(configure);

            return services;
        }
    }
}
