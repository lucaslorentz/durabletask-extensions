using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LLL.DurableTask.Api.Extensions;
using LLL.DurableTask.Api.Metadata;
using LLL.DurableTask.Api.Models;
using LLL.DurableTask.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.Api.Endpoints;

public static class EntrypointEndpoint
{
    public static IReadOnlyList<IEndpointConventionBuilder> MapEntrypointEndpoint(
        this IEndpointRouteBuilder builder,
        PathString prefix)
    {
        var endpoints = new List<IEndpointConventionBuilder>();

        endpoints.Add(builder.MapGet(prefix, async context =>
        {
            var response = new EntrypointResponse();

            var endpointDataSource = builder.ServiceProvider.GetRequiredService<EndpointDataSource>();
            var orchestrationServiceFeaturesClient = context.RequestServices.GetRequiredService<IOrchestrationServiceFeaturesClient>();

            response.Features = await orchestrationServiceFeaturesClient.GetFeatures();

            foreach (var endpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
            {
                var durableTaskEndpointMetadata = endpoint.Metadata.GetMetadata<DurableTaskEndpointMetadata>();

                if (durableTaskEndpointMetadata == null)
                    continue;

                var authorized = true;

                var policies = endpoint.Metadata.OfType<AuthorizeAttribute>()
                    .Select(a => a.Policy)
                    .Where(p => p != null)
                    .ToArray();

                if (policies.Length > 0)
                {
                    var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
                    var authorizeTasks = policies.Select(p => authorizationService.AuthorizeAsync(context.User, p));
                    authorized = (await Task.WhenAll(authorizeTasks)).All(r => r.Succeeded);
                }

                var httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();

                response.Endpoints[durableTaskEndpointMetadata.Id] = new EndpointInfo
                {
                    Href = endpoint.RoutePattern.RawText,
                    Method = httpMethodMetadata?.HttpMethods.FirstOrDefault(),
                    Authorized = authorized
                };
            }

            await context.RespondJson(response);
        }).RequireAuthorization(DurableTaskPolicy.Entrypoint).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "Entrypoint"
        }));

        return endpoints;
    }
}
