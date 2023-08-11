using System;
using LLL.DurableTask.Server.Configuration;
using LLL.DurableTask.Server.Grpc;
using LLL.DurableTask.Server.Grpc.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class TaskHubServerBuilderExtensions
{
    public static ITaskHubServerBuilder AddGrpcEndpoints(
        this ITaskHubServerBuilder builder,
        Action<GrpcServerOrchestrationServiceOptions> configure = null
    )
    {
        builder.Services.AddOptions<GrpcServerOrchestrationServiceOptions>();

        if (configure != null)
            builder.Services.Configure<GrpcServerOrchestrationServiceOptions>(configure);

        builder.Services.AddSingleton<GrpcServerOrchestrationService>();
        return builder;
    }
}
