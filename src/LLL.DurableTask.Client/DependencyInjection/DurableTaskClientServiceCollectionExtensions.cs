using System;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

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
                var jsonDataConverter = new JsonDataConverter(new JsonSerializerSettings());
                return new TaskHubClient(orchestrationServiceClient, jsonDataConverter);
            });

            return services;
        }
    }
}
