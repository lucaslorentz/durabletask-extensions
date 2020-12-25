using LLL.DurableTask.Api;
using LLL.DurableTask.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapDurableTaskApi(this IEndpointRouteBuilder builder)
        {
            return builder.MapDurableTaskApi("/api");
        }

        public static IEndpointConventionBuilder MapDurableTaskApi(this IEndpointRouteBuilder builder,
            PathString prefix)
        {
            var durableTaskEndpointConventionBuilder = new DurableTaskApiEndpointConventionBuilder();

            durableTaskEndpointConventionBuilder.AddEndpoints(builder.MapFeaturesEndpoints(prefix));
            durableTaskEndpointConventionBuilder.AddEndpoints(builder.MapOrchestrationEndpoints(prefix));

            return durableTaskEndpointConventionBuilder;
        }
    }
}
