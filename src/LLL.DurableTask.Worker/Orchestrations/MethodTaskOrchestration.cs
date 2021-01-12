using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Exceptions;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;
using LLL.DurableTask.Worker.Utils;
using DUtils = DurableTask.Core.Common.Utils;

namespace LLL.DurableTask.Worker.Orchestrations
{
    public class MethodTaskOrchestration : TaskOrchestration
    {
        private readonly MethodInfo _methodInfo;
        private readonly DataConverter _dataConverter;
        private OrchestrationEventReceiver _eventReceiver;

        public MethodTaskOrchestration(
            object instance,
            MethodInfo methodInfo)
        {
            Instance = instance;
            _methodInfo = methodInfo;
            _dataConverter = new TypelessJsonDataConverter();
        }

        public object Instance { get; }

        public override async Task<string> Execute(OrchestrationContext context, string input)
        {
            var parameters = PrepareParameters(input, new Dictionary<Type, Func<object>>
            {
                [typeof(OrchestrationContext)] = () => context,
                [typeof(OrchestrationEventReceiver)] = () => _eventReceiver = new OrchestrationEventReceiver(context),
                [typeof(OrchestrationGuidGenerator)] = () => new OrchestrationGuidGenerator(context.OrchestrationInstance.ExecutionId)
            });

            string serializedResult;
            try
            {
                var result = _methodInfo.Invoke(Instance, parameters);
                serializedResult = await SerializeResult(result);
            }
            catch (Exception e) when (!DUtils.IsFatal(e) && !DUtils.IsExecutionAborting(e))
            {
                var details = DUtils.SerializeCause(e, _dataConverter);
                throw new OrchestrationFailureException(e.Message, details);
            }
            return serializedResult;
        }

        public override string GetStatus()
        {
            return null;
        }

        public override void RaiseEvent(OrchestrationContext context, string name, string input)
        {
            _eventReceiver?.RaiseEvent(name, input);
        }

        private object[] PrepareParameters(
            string input,
            Dictionary<Type, Func<object>> factories)
        {
            var parameters = new List<object>();

            foreach (var p in _methodInfo.GetParameters())
            {
                object value;

                if (factories.TryGetValue(p.ParameterType, out var factory))
                {
                    value = factory();
                }
                else if (input != null)
                {
                    value = _dataConverter.Deserialize(input, p.ParameterType);
                }
                else if (p.HasDefaultValue)
                {
                    value = p.DefaultValue;
                }
                else
                    throw new Exception($"Orchestration input was not provided.");

                parameters.Add(value);
            }

            return parameters.ToArray();
        }

        private async Task<string> SerializeResult(object result)
        {
            if (result is Task task)
            {
                if (_methodInfo.ReturnType.IsGenericType)
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
