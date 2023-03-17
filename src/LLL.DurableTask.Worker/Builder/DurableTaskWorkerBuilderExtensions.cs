
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
        public static IDurableTaskWorkerBuilder AddAnnotatedFrom(
            this IDurableTaskWorkerBuilder builder,
            Assembly assembly)
        {
            return builder
                .AddAnnotatedOrchestrationsFrom(assembly)
                .AddAnnotatedActivitiesFrom(assembly);
        }

        public static IDurableTaskWorkerBuilder AddAnnotatedFrom(
            this IDurableTaskWorkerBuilder builder,
            Type type)
        {
            return builder
                .AddAnnotatedOrchestrationsFrom(type)
                .AddAnnotatedActivitiesFrom(type);
        }

        public static IDurableTaskWorkerBuilder AddAnnotatedOrchestrationsFrom(
            this IDurableTaskWorkerBuilder builder,
            Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                builder.AddAnnotatedOrchestrationsFrom(type);
            return builder;
        }

        public static IDurableTaskWorkerBuilder AddAnnotatedOrchestrationsFrom(
            this IDurableTaskWorkerBuilder builder,
            Type type)
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
            return builder.AddOrchestration(
                p => ActivatorUtilities.GetServiceOrCreateInstance(p, type) as TaskOrchestration,
                name ?? NameVersionHelper.GetDefaultName(type),
                version ?? NameVersionHelper.GetDefaultVersion(type));
        }

        public static IDurableTaskWorkerBuilder AddAnnotatedActivitiesFrom(
            this IDurableTaskWorkerBuilder builder,
            Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                builder.AddAnnotatedActivitiesFrom(type);
            return builder;
        }

        public static IDurableTaskWorkerBuilder AddAnnotatedActivitiesFrom(
            this IDurableTaskWorkerBuilder builder,
            Type type)
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
            return builder;
        }

        public static IDurableTaskWorkerBuilder AddActivitiesFromInterface<TService, TImplementation>(
            this IDurableTaskWorkerBuilder builder,
            bool useFullyQualifiedMethodNames = false)
            where TImplementation : TService
        {
            return builder.AddActivitiesFromInterface(typeof(TService), typeof(TImplementation), useFullyQualifiedMethodNames);
        }

        public static IDurableTaskWorkerBuilder AddActivitiesFromInterface(
            this IDurableTaskWorkerBuilder builder,
            Type interfaceType,
            Type implementationType,
            bool useFullyQualifiedMethodNames = false)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType} is not an interface", nameof(interfaceType));

            if (!interfaceType.IsAssignableFrom(implementationType))
                throw new ArgumentException($"{implementationType.FullName} does not implement {interfaceType.FullName}", nameof(implementationType));

            foreach (var methodInfo in interfaceType.GetMethods())
            {
                var name = NameVersionHelper.GetDefaultName(methodInfo, useFullyQualifiedMethodNames);
                var version = NameVersionHelper.GetDefaultVersion(methodInfo);
                builder.AddActivityMethod(
                    implementationType,
                    methodInfo,
                    name,
                    version);
            }

            return builder;
        }

        public static IDurableTaskWorkerBuilder AddActivityMethod(
            this IDurableTaskWorkerBuilder builder,
            Type serviceType,
            MethodInfo methodInfo,
            string name = null,
            string version = null)
        {
            return builder.AddActivity(
                p => new MethodTaskActivity(ActivatorUtilities.GetServiceOrCreateInstance(p, serviceType), methodInfo),
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