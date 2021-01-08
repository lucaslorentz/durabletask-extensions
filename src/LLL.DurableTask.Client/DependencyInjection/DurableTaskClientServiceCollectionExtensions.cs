using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DurableTaskClientServiceCollectionExtensions
    {
        public static IServiceCollection AddDurableTaskClient(
            this IServiceCollection services)
        {
            services.TryAddSingleton(serviceProvider =>
            {
                var orchestrationServiceClient = serviceProvider.GetRequiredService<IOrchestrationServiceClient>();
                var jsonDataConverter = new TypelessJsonDataConverter();
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                return new TaskHubClient(orchestrationServiceClient, jsonDataConverter, loggerFactory);
            });

            return services;
        }
    }
}
