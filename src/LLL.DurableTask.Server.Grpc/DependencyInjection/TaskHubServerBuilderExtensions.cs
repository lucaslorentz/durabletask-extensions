using LLL.DurableTask.Server.Configuration;
using LLL.DurableTask.Server.Grpc.Server;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TaskHubServerBuilderExtensions
    {
        public static ITaskHubServerBuilder AddGrpcEndpoints(
            this ITaskHubServerBuilder builder
        )
        {
            builder.Services.AddSingleton<GrpcServerOrchestrationService>();
            return builder;
        }
    }
}