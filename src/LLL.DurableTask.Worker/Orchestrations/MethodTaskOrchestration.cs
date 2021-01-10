using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;
using LLL.DurableTask.Worker.Utils;

namespace LLL.DurableTask.Worker.Orchestrations
{
    public class MethodTaskOrchestration : TaskOrchestration
    {
        private readonly MethodInfo _methodInfo;
        private readonly DataConverter _dataConverter;
        private readonly OrchestrationEventReceiver _eventReceiver;

        public MethodTaskOrchestration(
            object instance,
            MethodInfo methodInfo)
        {
            Instance = instance;
            _methodInfo = methodInfo;
            _dataConverter = new TypelessJsonDataConverter();
            _eventReceiver = new OrchestrationEventReceiver();
        }

        public object Instance { get; }

        public override async Task<string> Execute(OrchestrationContext context, string input)
        {
            var parameters = PrepareParameters(input, new Dictionary<Type, Func<object>>
            {
                [typeof(OrchestrationContext)] = () => context,
                [typeof(OrchestrationEventReceiver)] = () => _eventReceiver,
                [typeof(OrchestrationGuidGenerator)] = () => new OrchestrationGuidGenerator(context.OrchestrationInstance.ExecutionId)
            });

            var result = _methodInfo.Invoke(Instance, parameters);

            return await SerializeResult(result);
        }

        public override string GetStatus()
        {
            return null;
        }

        public override void RaiseEvent(OrchestrationContext context, string name, string input)
        {
            _eventReceiver.RaiseEvent(name, input);
        }

        private object[] PrepareParameters(
            string input,
            Dictionary<Type, Func<object>> factories)
        {
            return _methodInfo
                .GetParameters()
                .Select(p =>
                {
                    if (factories.TryGetValue(p.ParameterType, out var factory))
                        return factory();

                    if (input == null)
                        return null;

                    return _dataConverter.Deserialize(input, p.ParameterType);
                }).ToArray();
        }

        private async Task<string> SerializeResult(object result)
        {
            if (result is Task task)
            {
                if (task.GetType().IsGenericType)
                {
                    result = await (dynamic)task;
                }
                else
                {
                    await task;
                    result = null;
                }
            }

            var serializedResult = _dataConverter.Serialize(result);

            return serializedResult;
        }
    }
}
