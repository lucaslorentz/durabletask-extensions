using System;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Middleware;
using LLL.DurableTask.Worker.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.Worker.Middlewares;

public class ServiceProviderActivityMiddleware
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ServiceProviderActivityMiddleware(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Execute(DispatchMiddlewareContext context, Func<Task> next)
    {
        using var serviceScope = _serviceScopeFactory.CreateScope();
        context.SetProperty(serviceScope.ServiceProvider);

        if (context.GetProperty<TaskActivity>() is ServiceProviderTaskActivity initializable)
        {
            initializable.Initialize(serviceScope.ServiceProvider);
        }

        await next();
    }
}
