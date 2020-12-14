using System;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Middleware;
using LLL.DurableTask.Worker.Orchestrations;
using LLL.DurableTask.Worker.Services;

namespace LLL.DurableTask.Worker.Middlewares
{
    public class ServiceProviderOrchestrationMiddleware
    {
        public async Task Execute(DispatchMiddlewareContext context, Func<Task> next)
        {
            var orchestrationInstance = context.GetProperty<OrchestrationInstance>();

            var serviceScope = WorkerOrchestrationService.OrchestrationsServiceScopes[orchestrationInstance.InstanceId];

            context.SetProperty(serviceScope.ServiceProvider);

            if (context.GetProperty<TaskOrchestration>() is ServiceProviderTaskOrchestration initializable)
            {
                initializable.Initialize(serviceScope.ServiceProvider);
            }

            await next();
        }
    }
}
