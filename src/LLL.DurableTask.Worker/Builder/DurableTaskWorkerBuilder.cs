using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Middleware;
using LLL.DurableTask.Core;
using LLL.DurableTask.Worker.Activities;
using LLL.DurableTask.Worker.Middlewares;
using LLL.DurableTask.Worker.ObjectCreators;
using LLL.DurableTask.Worker.Orchestrations;
using LLL.DurableTask.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LLL.DurableTask.Worker.Builder;

public class DurableTaskWorkerBuilder : IDurableTaskWorkerBuilder
{
    private readonly List<Func<IServiceProvider, Func<DispatchMiddlewareContext, Func<Task>, Task>>> _orchestrationMiddlewares
        = new();

    private readonly List<Func<IServiceProvider, Func<DispatchMiddlewareContext, Func<Task>, Task>>> _activitiesMiddlewares
        = new();

    public IServiceCollection Services { get; }

    public DurableTaskWorkerBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public DurableTaskWorkerBuilder AddOrchestration(
        Func<IServiceProvider, TaskOrchestration> factory,
        string name,
        string version)
    {
        Services.AddSingleton<ObjectCreator<TaskOrchestration>>(new FactoryObjectCreator<TaskOrchestration>(
            name,
            version,
            () => new ServiceProviderTaskOrchestration(factory)));

        return this;
    }

    public DurableTaskWorkerBuilder AddActivity(
        Func<IServiceProvider, TaskActivity> factory,
        string name,
        string version)
    {
        Services.AddSingleton<ObjectCreator<TaskActivity>>(new FactoryObjectCreator<TaskActivity>(
            name,
            version,
            () => new ServiceProviderTaskActivity(factory)));

        return this;
    }

    public DurableTaskWorkerBuilder AddOrchestrationDispatcherMiddleware(
        Func<IServiceProvider, Func<DispatchMiddlewareContext, Func<Task>, Task>> factory)
    {
        _orchestrationMiddlewares.Add(factory);
        return this;
    }

    public DurableTaskWorkerBuilder AddActivityDispatcherMiddleware(
        Func<IServiceProvider, Func<DispatchMiddlewareContext, Func<Task>, Task>> factory)
    {
        _activitiesMiddlewares.Add(factory);
        return this;
    }

    public bool HasAllOrchestrations { get; set; }
    public bool HasAllActivities { get; set; }

    internal TaskHubWorker Build(IServiceProvider provider)
    {
        var orchestrationService = provider.GetRequiredService<IOrchestrationService>();
        var distributedOrchestrationService = provider.GetService<IDistributedOrchestrationService>();
        var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var orchestrations = provider.GetServices<ObjectCreator<TaskOrchestration>>().ToArray();
        var activities = provider.GetServices<ObjectCreator<TaskActivity>>().ToArray();

        var serviceProviderOrchestrationService = new WorkerOrchestrationService(
            orchestrationService,
            distributedOrchestrationService,
            serviceScopeFactory,
            orchestrations,
            activities,
            HasAllOrchestrations,
            HasAllActivities);

        var loggerFactory = provider.GetService<ILoggerFactory>();

        var taskHubWorker = new TaskHubWorker(serviceProviderOrchestrationService, loggerFactory);

        // Orchestration middlewares
        var serviceProviderOrchestrationMiddleware = provider.GetRequiredService<ServiceProviderOrchestrationMiddleware>();
        taskHubWorker.AddOrchestrationDispatcherMiddleware(serviceProviderOrchestrationMiddleware.Execute);
        foreach (var middleware in _orchestrationMiddlewares)
            taskHubWorker.AddOrchestrationDispatcherMiddleware(CreateMiddleware(middleware));

        // Activitie middlewares
        var serviceProviderActivityMiddleware = provider.GetRequiredService<ServiceProviderActivityMiddleware>();
        taskHubWorker.AddActivityDispatcherMiddleware(serviceProviderActivityMiddleware.Execute);
        foreach (var factory in _activitiesMiddlewares)
            taskHubWorker.AddActivityDispatcherMiddleware(CreateMiddleware(factory));

        // Orchestrations and activities
        taskHubWorker.AddTaskOrchestrations(orchestrations);
        taskHubWorker.AddTaskActivities(activities);

        taskHubWorker.ErrorPropagationMode = ErrorPropagationMode.UseFailureDetails;

        return taskHubWorker;
    }

    private Func<DispatchMiddlewareContext, Func<Task>, Task> CreateMiddleware(
        Func<IServiceProvider, Func<DispatchMiddlewareContext, Func<Task>, Task>> middlewareFactory)
    {
        return async (context, next) =>
        {
            var serviceProvider = context.GetProperty<IServiceProvider>()
                ?? throw new Exception("No service provider in dispatchMiddlewareContext");

            var middleware = middlewareFactory(serviceProvider);

            await middleware(context, next);
        };
    }
}
