using System;
using DurableTask.Core;
using LLL.DurableTask.Worker.Builder;
using LLL.DurableTask.Worker.Middlewares;
using LLL.DurableTask.Worker.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DurableTaskWorkerServiceCollectionExtensions
    {
        public static IServiceCollection AddDurableTaskWorker(
            this IServiceCollection services,
            Action<IDurableTaskWorkerBuilder> config = null)
        {
            services.AddHostedService<WorkerHostedService>();

            services.TryAddSingleton<ServiceProviderOrchestrationMiddleware>();
            services.TryAddSingleton<ServiceProviderActivityMiddleware>();

            var builder = new DurableTaskWorkerBuilder(services);

            config?.Invoke(builder);

            services.TryAddSingleton<TaskHubWorker>(provider => builder.Build(provider));

            return services;
        }
    }
}
