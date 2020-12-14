
using System;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Middleware;
using LLL.DurableTask.Worker.Orchestrations;
using LLL.DurableTask.Worker.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DurableTaskWorkerBuilderExtensions
    {
        public static IDurableTaskWorkerBuilder AddOrchestrationMethod(
            this IDurableTaskWorkerBuilder builder,
            Type type,
            string methodName,
            string name = null,
            string version = null)
        {
            var methodInfo = type.GetMethod(methodName);

            return builder.AddOrchestration(
                p => new ReflectionTaskOrchestration(ActivatorUtilities.GetServiceOrCreateInstance(p, type), methodInfo),
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

        public static IDurableTaskWorkerBuilder AddActivityMethod(
            this IDurableTaskWorkerBuilder builder,
            Type type,
            string methodName,
            string name = null,
            string version = null)
        {
            var methodInfo = type.GetMethod(methodName);

            return builder.AddActivity(
                p => new ReflectionTaskActivity(ActivatorUtilities.GetServiceOrCreateInstance(p, type), methodInfo),
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