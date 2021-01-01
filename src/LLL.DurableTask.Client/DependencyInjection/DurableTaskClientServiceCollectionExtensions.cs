using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                var jsonDataConverter = new UntypedJsonDataConverter();
                return new TaskHubClient(orchestrationServiceClient, jsonDataConverter);
            });

            return services;
        }
    }
}
