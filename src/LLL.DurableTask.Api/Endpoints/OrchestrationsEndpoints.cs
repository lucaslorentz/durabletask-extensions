using System;
using System.Collections.Generic;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.Api.Converters;
using LLL.DurableTask.Api.Extensions;
using LLL.DurableTask.Api.Metadata;
using LLL.DurableTask.Api.Models;
using LLL.DurableTask.Core;
using LLL.DurableTask.Server.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace LLL.DurableTask.Api.Endpoints
{
    public static class OrchestrationsEndpoints
    {
        public static IReadOnlyList<IEndpointConventionBuilder> MapOrchestrationEndpoints(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder,
            PathString prefix)
        {
            var endpoints = new List<IEndpointConventionBuilder>();

            endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations", async context =>
            {
                var extendedOrchestrationServiceClient = context.RequestServices.GetRequiredService<IExtendedOrchestrationServiceClient>();

                var query = context.ParseQuery<OrchestrationQuery>();

                var result = await extendedOrchestrationServiceClient.GetOrchestrationsAsync(query);

                await context.RespondJson(result);
            }).RequireAuthorization(DurableTaskPolicy.Read).WithMetadata(new DurableTaskEndpointMetadata
            {
                Id = "OrchestrationsList"
            }));

            endpoints.Add(builder.MapPost(prefix + "/v1/orchestrations", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var request = await context.ParseBody<CreateOrchestrationRequest>();

                var instance = await taskHubClient.CreateOrchestrationInstanceAsync(
                    request.Name,
                    request.Version ?? string.Empty,
                    request.InstanceId,
                    request.Input,
                    request.Tags);

                var typedHeaders = context.Response.GetTypedHeaders();
                typedHeaders.Location = new Uri($"/v1/orchestrations/{instance.InstanceId}", System.UriKind.Relative);

                await context.RespondJson(instance, 201);
            }).RequireAuthorization(DurableTaskPolicy.Create).WithMetadata(new DurableTaskEndpointMetadata
            {
                Id = "OrchestrationsCreate"
            }));

            endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations/{instanceId}", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();
                var state = await taskHubClient.GetOrchestrationStateAsync(instanceId);

                await context.RespondJson(state);
            }).RequireAuthorization(DurableTaskPolicy.Read).WithMetadata(new DurableTaskEndpointMetadata
            {
                Id = "OrchestrationsGet"
            }));

            endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations/{instanceId}/{executionId}", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();
                var executionId = context.Request.RouteValues["executionId"].ToString();

                var state = await taskHubClient.GetOrchestrationStateAsync(instanceId, executionId);

                await context.RespondJson(state);
            }).RequireAuthorization(DurableTaskPolicy.Read).WithMetadata(new DurableTaskEndpointMetadata
            {
                Id = "OrchestrationsGetExecution"
            }));

            endpoints.Add(builder.MapGet(prefix + "/v1/orchestrations/{instanceId}/{executionId}/history", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();
                var executionId = context.Request.RouteValues["executionId"].ToString();

                var orchestrationInstance = new OrchestrationInstance
                {
                    InstanceId = instanceId,
                    ExecutionId = executionId
                };

                var history = await taskHubClient.GetOrchestrationHistoryAsync(orchestrationInstance);

                var events = JsonConvert.DeserializeObject<HistoryEvent[]>(history, new JsonSerializerSettings
                {
                    Converters = { new HistoryEventConverter() }
                });

                await context.RespondJson(events);
            }).RequireAuthorization(DurableTaskPolicy.ReadHistory).WithMetadata(new DurableTaskEndpointMetadata
            {
                Id = "OrchestrationsGetExecutionHistory"
            }));

            endpoints.Add(builder.MapPost(prefix + "/v1/orchestrations/{instanceId}/terminate", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();

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

            endpoints.Add(builder.MapPost(prefix + "/v1/orchestrations/{instanceId}/raiseevent", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();

                var orchestrationInstance = new OrchestrationInstance
                {
                    InstanceId = instanceId
                };

                var request = await context.ParseBody<RaiseEventRequest>();

                await taskHubClient.RaiseEventAsync(orchestrationInstance, request.EventName, request.EventData);

                await context.RespondJson(new { });
            }).RequireAuthorization(DurableTaskPolicy.RaiseEvent).WithMetadata(new DurableTaskEndpointMetadata
            {
                Id = "OrchestrationsRaiseEvent"
            }));

            endpoints.Add(builder.MapDelete(prefix + "/v1/orchestrations/{instanceId}", async context =>
            {
                var extendedOrchestrationServiceClient = context.RequestServices.GetRequiredService<IExtendedOrchestrationServiceClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();

                var result = await extendedOrchestrationServiceClient.PurgeInstanceHistoryAsync(instanceId);

                await context.RespondJson(new { });
            }).RequireAuthorization(DurableTaskPolicy.Purge).WithMetadata(new DurableTaskEndpointMetadata
            {
                Id = "OrchestrationsPurgeInstance"
            }));

            return endpoints;
        }
    }
}
