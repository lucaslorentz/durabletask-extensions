using LLL.DurableTask.Server.Grpc.Server;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static GrpcServiceEndpointConventionBuilder MapDurableTaskServerGrpcService(
            this IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGrpcService<GrpcServerOrchestrationService>();
        }
    }
}
