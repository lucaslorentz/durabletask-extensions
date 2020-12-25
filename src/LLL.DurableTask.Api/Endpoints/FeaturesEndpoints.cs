using System.Collections.Generic;
using LLL.DurableTask.Api.Extensions;
using LLL.DurableTask.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.Api.Endpoints
{
    public static class FeaturesEndpoints
    {
        public static IReadOnlyList<IEndpointConventionBuilder> MapFeaturesEndpoints(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder,
            PathString prefix)
        {
            var endpoints = new List<IEndpointConventionBuilder>();

            endpoints.Add(builder.MapGet(prefix + "/v1/features", async context =>
            {
                var extendedOrchestrationServiceClient = context.RequestServices.GetRequiredService<IExtendedOrchestrationServiceClient>();

                var features = await extendedOrchestrationServiceClient.GetFeatures();

                await context.RespondJson(features);
            }).WithDisplayName("Get Features").RequireAuthorization(DurableTaskPolicy.Read));

            return endpoints;
        }
    }
}
