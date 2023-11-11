using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DurableTask.Core;
using DurableTask.Core.History;
using DurableTask.Core.Query;
using LLL.DurableTask.Api.Extensions;
using LLL.DurableTask.Api.Metadata;
using LLL.DurableTask.Api.Models;
using LLL.DurableTask.Core;
using LLL.DurableTask.Core.Serializing;
using LLL.DurableTask.Server.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace LLL.DurableTask.Api.Endpoints;

public static class OrchestrationsEndpoints
{
    public static IReadOnlyList<IEndpointConventionBuilder> MapOrchestrationEndpoints(
        this IEndpointRouteBuilder builder,
        PathString prefix)
    {
        var endpoints = new List<IEndpointConventionBuilder>();

        endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations", async context =>
        {
            var orchestrationServiceSearchClient = context.RequestServices.GetRequiredService<IOrchestrationServiceQueryClient>();

            var query = context.ParseQuery<OrchestrationQueryExtended>();

            var result = await orchestrationServiceSearchClient.GetOrchestrationWithQueryAsync(query, context.RequestAborted);

            await context.RespondJson(result);
        }).RequireAuthorization(DurableTaskPolicy.Read).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsList"
        }));

        endpoints.Add(builder.MapPost(prefix + "/v1/orchestrations", async context =>
        {
            var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();
            var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();

            var request = await context.ParseBody<CreateOrchestrationRequest>();

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults))
            {
                await context.RespondJson(validationResults, 400);
                return;
            }

            var instance = await taskHubClient.CreateOrchestrationInstanceAsync(
                request.Name,
                request.Version ?? string.Empty,
                request.InstanceId,
                request.Input,
                request.Tags);

            var typedHeaders = context.Response.GetTypedHeaders();
            typedHeaders.Location = new Uri(linkGenerator.GetUriByName("DurableTaskApi_OrchestrationsGet", new
            {
                instanceId = instance.InstanceId
            }, context.Request.Scheme, context.Request.Host, context.Request.PathBase));

            await context.RespondJson(instance, 201);
        }).RequireAuthorization(DurableTaskPolicy.Create).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsCreate"
        }));

        endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations/{instanceId}", async context =>
        {
            var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

            var instanceId = Uri.UnescapeDataString(context.Request.RouteValues["instanceId"].ToString());

            var state = await taskHubClient.GetOrchestrationStateAsync(instanceId);

            if (state == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            await context.RespondJson(state);
        }).RequireAuthorization(DurableTaskPolicy.Read)
        .WithMetadata(new EndpointNameMetadata("DurableTaskApi_OrchestrationsGet"))
        .WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsGet"
        }));

        endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations/{instanceId}/{executionId}", async context =>
        {
            var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

            var instanceId = Uri.UnescapeDataString(context.Request.RouteValues["instanceId"].ToString());
            var executionId = Uri.UnescapeDataString(context.Request.RouteValues["executionId"].ToString());

            var state = await taskHubClient.GetOrchestrationStateAsync(instanceId, executionId);

            if (state == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            await context.RespondJson(state);
        }).RequireAuthorization(DurableTaskPolicy.Read)
        .WithMetadata(new EndpointNameMetadata("DurableTaskApi_OrchestrationsGetExecution"))
        .WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsGetExecution"
        }));

        endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations/{instanceId}/{executionId}/history", async context =>
        {
            var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

            var instanceId = Uri.UnescapeDataString(context.Request.RouteValues["instanceId"].ToString());
            var executionId = Uri.UnescapeDataString(context.Request.RouteValues["executionId"].ToString());

            var orchestrationInstance = new OrchestrationInstance
            {
                InstanceId = instanceId,
                ExecutionId = executionId
            };

            var history = await taskHubClient.GetOrchestrationHistoryAsync(orchestrationInstance);

            var events = new TypelessJsonDataConverter().Deserialize<HistoryEvent[]>(history);

            await context.RespondJson(events);
        }).RequireAuthorization(DurableTaskPolicy.ReadHistory).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsGetExecutionHistory"
        }));

        endpoints.Add(builder.MapPost(prefix + "/v1/orchestrations/{instanceId}/terminate", async context =>
        {
            var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

            var instanceId = Uri.UnescapeDataString(context.Request.RouteValues["instanceId"].ToString());

            var orchestrationInstance = new OrchestrationInstance
            {
                InstanceId = instanceId
            };

            var request = await context.ParseBody<TerminateRequest>();

            await taskHubClient.TerminateInstanceAsync(orchestrationInstance, request.Reason);

            await context.RespondJson(new { });
        }).RequireAuthorization(DurableTaskPolicy.Terminate).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsTerminate"
        }));

        endpoints.Add(builder.MapPost(prefix + "/v1/orchestrations/{instanceId}/rewind", async context =>
        {
            var orchestrationServiceRewindClient = context.RequestServices.GetRequiredService<IOrchestrationServiceRewindClient>();

            var instanceId = Uri.UnescapeDataString(context.Request.RouteValues["instanceId"].ToString());

            var request = await context.ParseBody<RewindRequest>();

            await orchestrationServiceRewindClient.RewindTaskOrchestrationAsync(instanceId, request.Reason);

            await context.RespondJson(new { });
        }).RequireAuthorization(DurableTaskPolicy.Rewind).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsRewind"
        }));

        endpoints.Add(builder.MapPost(prefix + "/v1/orchestrations/{instanceId}/raiseevent/{eventName}", async context =>
        {
            var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

            var instanceId = Uri.UnescapeDataString(context.Request.RouteValues["instanceId"].ToString());
            var eventName = Uri.UnescapeDataString(context.Request.RouteValues["eventName"].ToString());

            var orchestrationInstance = new OrchestrationInstance
            {
                InstanceId = instanceId
            };

            var eventData = await context.ParseBody<JToken>();

            await taskHubClient.RaiseEventAsync(orchestrationInstance, eventName, eventData);

            await context.RespondJson(new { });
        }).RequireAuthorization(DurableTaskPolicy.RaiseEvent).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsRaiseEvent"
        }));

        endpoints.Add(builder.MapDelete(prefix + "/v1/orchestrations/{instanceId}", async context =>
        {
            var orchestrationServicePurgeClient = context.RequestServices.GetRequiredService<IOrchestrationServicePurgeClient>();

            var instanceId = Uri.UnescapeDataString(context.Request.RouteValues["instanceId"].ToString());

            var result = await orchestrationServicePurgeClient.PurgeInstanceStateAsync(instanceId);

            await context.RespondJson(new { });
        }).RequireAuthorization(DurableTaskPolicy.Purge).WithMetadata(new DurableTaskEndpointMetadata
        {
            Id = "OrchestrationsPurgeInstance"
        }));

        return endpoints;
    }
}
