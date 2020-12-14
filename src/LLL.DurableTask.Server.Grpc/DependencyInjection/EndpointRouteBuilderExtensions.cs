using LLL.DurableTask.Server.Grpc.Server;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapTaskHubServerGrpcEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGrpcService<GrpcServerOrchestrationService>();
            
            return endpoints;
        }
    }
}
