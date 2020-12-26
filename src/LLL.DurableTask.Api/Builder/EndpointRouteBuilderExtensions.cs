using System.Linq;
using LLL.DurableTask.Api;
using LLL.DurableTask.Api.DependencyInjection;
using LLL.DurableTask.Api.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            var options = builder.ServiceProvider.GetRequiredService<IOptions<DurableTaskApiOptions>>().Value;

            var durableTaskEndpointConventionBuilder = new DurableTaskApiEndpointConventionBuilder();

            durableTaskEndpointConventionBuilder.AddEndpoints(builder.MapEntrypointEndpoint(prefix));
            durableTaskEndpointConventionBuilder.AddEndpoints(builder.MapOrchestrationEndpoints(prefix));

            if (options.DisableAuthorization)
            {
                durableTaskEndpointConventionBuilder.Add(builder =>
                {
                    foreach (var authorizeAttribute in builder.Metadata.OfType<AuthorizeAttribute>().ToArray())
                    {
                        builder.Metadata.Remove(authorizeAttribute);
                    }
                });
            }

            return durableTaskEndpointConventionBuilder;
        }
    }
}
