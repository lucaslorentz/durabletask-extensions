using System;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.Api;
using LLL.DurableTask.Api.Converters;
using LLL.DurableTask.Api.Models;
using LLL.DurableTask.Core;
using LLL.DurableTask.Server.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcBuilderExtensions
    {
        public static void MapDurableTaskUiApi(this AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/v1/features", async context =>
            {
                var extendedOrchestrationServiceClient = context.RequestServices.GetRequiredService<IExtendedOrchestrationServiceClient>();

                await context.RespondJson(extendedOrchestrationServiceClient.Features);
            });

            endpoints.MapGet("/api/v1/orchestrations", async context =>
            {
                var extendedOrchestrationServiceClient = context.RequestServices.GetRequiredService<IExtendedOrchestrationServiceClient>();

                var query = context.ParseQuery<OrchestrationQuery>();

                var result = await extendedOrchestrationServiceClient.GetOrchestrationsAsync(query);

                await context.RespondJson(result);
            });

            endpoints.MapPost("/api/v1/orchestrations", async context =>
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
                typedHeaders.Location = new Uri($"/api/v1/orchestrations/{instance.InstanceId}", System.UriKind.Relative);

                await context.RespondJson(instance, 201);
            });

            endpoints.MapGet("/api/v1/orchestrations/{instanceId}", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();
                var state = await taskHubClient.GetOrchestrationStateAsync(instanceId);

                await context.RespondJson(state);
            });

            endpoints.MapGet("/api/v1/orchestrations/{instanceId}/{executionId}", async context =>
            {
                var taskHubClient = context.RequestServices.GetRequiredService<TaskHubClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();
                var executionId = context.Request.RouteValues["executionId"].ToString();

                var state = await taskHubClient.GetOrchestrationStateAsync(instanceId, executionId);

                await context.RespondJson(state);
            });

            endpoints.MapGet("/api/v1/orchestrations/{instanceId}/{executionId}/history", async context =>
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
            });

            endpoints.MapPost("/api/v1/orchestrations/{instanceId}/terminate", async context =>
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
            });

            endpoints.MapPost("/api/v1/orchestrations/{instanceId}/raiseevent", async context =>
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
            });

            endpoints.MapDelete("/api/v1/orchestrations/{instanceId}", async context =>
            {
                var extendedOrchestrationServiceClient = context.RequestServices.GetRequiredService<IExtendedOrchestrationServiceClient>();

                var instanceId = context.Request.RouteValues["instanceId"].ToString();

                var result = await extendedOrchestrationServiceClient.PurgeInstanceHistoryAsync(instanceId);

                await context.RespondJson(new { });
            });
        }
    }
}
