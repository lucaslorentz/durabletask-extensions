using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;
using Newtonsoft.Json.Linq;

namespace LLL.DurableTask.Worker.Orchestrations
{
    public class MethodTaskActivity : TaskActivity
    {
        private readonly MethodInfo _methodInfo;
        private readonly DataConverter _dataConverter;

        public MethodTaskActivity(
            object instance,
            MethodInfo methodInfo)
        {
            Instance = instance;
            _methodInfo = methodInfo;
            _dataConverter = new TypelessJsonDataConverter();
        }

        public object Instance { get; }

        public override string Run(TaskContext context, string input)
        {
            throw new NotImplementedException();
        }

        public override async Task<string> RunAsync(TaskContext context, string input)
        {
            var parameters = PrepareParameters(input, new Dictionary<Type, Func<object>>
            {
                [typeof(TaskContext)] = () => context
            });

            var result = _methodInfo.Invoke(Instance, parameters);

            return await SerializeResult(result);
        }

        private object[] PrepareParameters(
            string input,
            Dictionary<Type, Func<object>> factories)
        {
            var deserializedInput = _dataConverter.Deserialize<JToken[]>(input);

            var inputPosition = 0;

            return _methodInfo
                .GetParameters()
                .Select(p =>
                {
                    if (factories.TryGetValue(p.ParameterType, out var factory))
                        return factory();

                    if (input == null)
                        return null;

                    return deserializedInput[inputPosition++].ToObject(p.ParameterType);
                }).ToArray();
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
