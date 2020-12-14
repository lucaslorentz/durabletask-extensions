using System;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.Worker.Builder
{
    public interface IDurableTaskWorkerBuilder
    {
        IServiceCollection Services { get; }

        bool HasAllOrchestrations { get; set; }
        bool HasAllActivities { get; set; }
        
        DurableTaskWorkerBuilder AddOrchestration(Func<IServiceProvider, TaskOrchestration> factory, string name = null, string version = null);

        DurableTaskWorkerBuilder AddActivity(Func<IServiceProvider, TaskActivity> factory, string name = null, string version = null);

        DurableTaskWorkerBuilder AddOrchestrationDispatcherMiddleware(Func<IServiceProvider, Func<DispatchMiddlewareContext, Func<Task>, Task>> factory);

        DurableTaskWorkerBuilder AddActivityDispatcherMiddleware(Func<IServiceProvider, Func<DispatchMiddlewareContext, Func<Task>, Task>> factory);
    }
}
