using DurableTask.Core;
using LLL.DurableTask.Emulator;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EmulatorServiceCollectionExtensions
    {
        public static IServiceCollection AddDurableTaskEmulatorStorage(this IServiceCollection services)
        {
            services.AddSingleton<DisposeSafeLocalOrchestrationService>();
            services.AddSingleton<IOrchestrationServiceClient>(p => p.GetService<DisposeSafeLocalOrchestrationService>());
            services.AddSingleton<IOrchestrationService>(p => p.GetService<DisposeSafeLocalOrchestrationService>());
            return services;
        }
    }
}
