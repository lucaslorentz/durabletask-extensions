
using System;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Middleware;
using LLL.DurableTask.Worker.Orchestrations;
using LLL.DurableTask.Worker.Builder;
using System.Reflection;
using LLL.DurableTask.Worker.Attributes;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DurableTaskWorkerBuilderExtensions
    {
        public static IDurableTaskWorkerBuilder AddFromAssembly(
            this IDurableTaskWorkerBuilder builder,
            Assembly assembly)
        {
            return builder
                .AddOrchestrationsFromAssembly(assembly)
                .AddActivitiesFromAssembly(assembly);
        }

        public static IDurableTaskWorkerBuilder AddOrchestrationsFromAssembly(
            this IDurableTaskWorkerBuilder builder,
            Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var typeOrchestrationAttribute = type.GetCustomAttribute<OrchestrationAttribute>();
                if (typeOrchestrationAttribute != null)
                {
                    builder.AddOrchestration(type, typeOrchestrationAttribute.Name, typeOrchestrationAttribute.Version);
                }

                foreach (var methodInfo in type.GetMethods())
                {
                    var orchestrationAttribute = methodInfo.GetCustomAttribute<OrchestrationAttribute>();
                    if (orchestrationAttribute != null)
                    {
                        builder.AddOrchestrationMethod(type, methodInfo, orchestrationAttribute.Name, orchestrationAttribute.Version);
                    }
                }
            }
            return builder;
        }

        public static IDurableTaskWorkerBuilder AddOrchestrationMethod(
            this IDurableTaskWorkerBuilder builder,
            Type type,
            MethodInfo methodInfo,
            string name = null,
            string version = null)
        {
            return builder.AddOrchestration(
                p => new MethodTaskOrchestration(ActivatorUtilities.GetServiceOrCreateInstance(p, type), methodInfo),
                name ?? NameVersionHelper.GetDefaultName(methodInfo),
                version ?? NameVersionHelper.GetDefaultVersion(methodInfo));
        }

        public static IDurableTaskWorkerBuilder AddOrchestration<T>(
            this IDurableTaskWorkerBuilder builder,
            string name = null,
            string version = null)
            where T : TaskOrchestration
        {
            return builder.AddOrchestration(typeof(T), name, version);
        }

        public static IDurableTaskWorkerBuilder AddOrchestration(
            this IDurableTaskWorkerBuilder builder,
            Type type,
            string name = null,
            string version = null)
        {
            builder.Services.AddScoped(type, type);

            return builder.AddOrchestration(
                p => ActivatorUtilities.GetServiceOrCreateInstance(p, type) as TaskOrchestration,
                name ?? NameVersionHelper.GetDefaultName(type),
                version ?? NameVersionHelper.GetDefaultVersion(type));
        }

        public static IDurableTaskWorkerBuilder AddActivitiesFromAssembly(
            this IDurableTaskWorkerBuilder builder,
            Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var typeActivityAttribute = type.GetCustomAttribute<ActivityAttribute>();
                if (typeActivityAttribute != null)
                {
                    builder.AddActivity(type, typeActivityAttribute.Name, typeActivityAttribute.Version);
                }

                foreach (var methodInfo in type.GetMethods())
                {
                    var methodActivityAttribute = methodInfo.GetCustomAttribute<ActivityAttribute>();
                    if (methodActivityAttribute != null)
                    {
                        builder.AddActivityMethod(type, methodInfo, methodActivityAttribute.Name, methodActivityAttribute.Version);
                    }
                }
            }
            return builder;
        }

        public static IDurableTaskWorkerBuilder AddActivityMethod(
            this IDurableTaskWorkerBuilder builder,
            Type type,
            MethodInfo methodInfo,
            string name = null,
            string version = null)
        {
            return builder.AddActivity(
                p => new MethodTaskActivity(ActivatorUtilities.GetServiceOrCreateInstance(p, type), methodInfo),
                name ?? NameVersionHelper.GetDefaultName(methodInfo),
                version ?? NameVersionHelper.GetDefaultVersion(methodInfo));
        }

        public static IDurableTaskWorkerBuilder AddActivity<T>(
            this IDurableTaskWorkerBuilder builder,
            string name = null,
            string version = null)
            where T : TaskActivity
        {
            return builder.AddActivity(typeof(T), name, version);
        }

        public static IDurableTaskWorkerBuilder AddActivity(
            this IDurableTaskWorkerBuilder builder,
            Type type,
            string name = null,
            string version = null)
        {
            builder.Services.AddScoped(type, type);

            return builder.AddActivity(
                p => ActivatorUtilities.GetServiceOrCreateInstance(p, type) as TaskActivity,
                name ?? NameVersionHelper.GetDefaultName(type),
                version ?? NameVersionHelper.GetDefaultVersion(type));
        }

        public static IDurableTaskWorkerBuilder AddOrchestrationDispatcherMiddleware(
            this IDurableTaskWorkerBuilder builder,
            Func<DispatchMiddlewareContext, Func<Task>, Task> middleware)
        {
            return builder.AddOrchestrationDispatcherMiddleware(_ => middleware);
        }

        public static IDurableTaskWorkerBuilder AddActivityDispatcherMiddleware(
            this IDurableTaskWorkerBuilder builder,
            Func<DispatchMiddlewareContext, Func<Task>, Task> middleware)
        {
            return builder.AddActivityDispatcherMiddleware(_ => middleware);
        }
    }
}