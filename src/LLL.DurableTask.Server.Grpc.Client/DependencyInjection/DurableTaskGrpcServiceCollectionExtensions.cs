using System;
using DurableTask.Core;
using DurableTask.Core.Query;
using LLL.DurableTask.Core;
using LLL.DurableTask.Server.Client;
using Microsoft.Extensions.Options;
using static DurableTaskGrpc.OrchestrationService;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DurableTaskGrpcServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddDurableTaskServerStorageGrpc(
            this IServiceCollection services,
            Action<GrpcClientOrchestrationServiceOptions> configure)
        {
            services.AddOptions<GrpcClientOrchestrationServiceOptions>()
                .Configure(configure);

            services.AddSingleton<GrpcClientOrchestrationService>();
            services.AddSingleton<IOrchestrationService>(p => p.GetRequiredService<GrpcClientOrchestrationService>());
            services.AddSingleton<IDistributedOrchestrationService>(p => p.GetRequiredService<GrpcClientOrchestrationService>());
            services.AddSingleton<IOrchestrationServiceClient>(p => p.GetRequiredService<GrpcClientOrchestrationService>());
            services.AddSingleton<IOrchestrationServiceQueryClient>(p => p.GetRequiredService<GrpcClientOrchestrationService>());
            services.AddSingleton<IOrchestrationServicePurgeClient>(p => p.GetRequiredService<GrpcClientOrchestrationService>());
            services.AddSingleton<IOrchestrationServiceFeaturesClient>(p => p.GetRequiredService<GrpcClientOrchestrationService>());
            services.AddSingleton<IOrchestrationServiceRewindClient>(p => p.GetRequiredService<GrpcClientOrchestrationService>());

            return services.AddGrpcClient<OrchestrationServiceClient>((s, o) =>
            {
                var options = s.GetRequiredService<IOptions<GrpcClientOrchestrationServiceOptions>>().Value;
                o.Address = options.BaseAddress;
            });
        }
    }
}
