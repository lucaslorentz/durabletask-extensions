using System;
using DurableTask.Core;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.EFCore.DependencyInjection;
using LLL.DurableTask.EFCore.Mappers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DurableTaskEFCoreServiceCollectionExtensions
    {
        public static IEFCoreOrchestrationBuilder AddDurableTaskEFCoreStorage(
            this IServiceCollection services,
            Action<EFCoreOrchestrationOptions> configure = null)
        {
            var builder = new EFCoreOrchestrationBuilder(services);

            services.AddOptions<EFCoreOrchestrationOptions>();

            if (configure != null)
                services.Configure<EFCoreOrchestrationOptions>(configure);

            services.AddDbContext<OrchestrationDbContext>(options =>
            {
                foreach (var configuration in builder.DbContextConfigurations)
                {
                    configuration(options);
                }
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);

            services.AddSingleton<Func<OrchestrationDbContext>>(p => () => p.GetRequiredService<OrchestrationDbContext>());

            services.AddSingleton<EFCoreOrchestrationService>();
            services.AddSingleton<IExtendedOrchestrationService>(p => p.GetRequiredService<EFCoreOrchestrationService>());
            services.AddSingleton<IOrchestrationService>(p => p.GetRequiredService<EFCoreOrchestrationService>());
            services.AddSingleton<IOrchestrationServiceClient>(p => p.GetRequiredService<EFCoreOrchestrationService>());
            services.AddSingleton<IExtendedOrchestrationServiceClient>(p => p.GetRequiredService<EFCoreOrchestrationService>());

            services.AddSingleton<OrchestrationMessageMapper>();
            services.AddSingleton<ActivityMessageMapper>();
            services.AddSingleton<InstanceMapper>();
            services.AddSingleton<ExecutionMapper>();

            return builder;
        }
    }
}
