using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;
using Newtonsoft.Json;
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
            var parameters = PrepareParameters(input);

            var result = _methodInfo.Invoke(Instance, parameters);

            return await SerializeResult(result);
        }

        private object[] PrepareParameters(string input)
        {
            var deserializedInput = JsonConvert.DeserializeObject<JToken[]>(input);

            return _methodInfo
                .GetParameters()
                .Select(p =>
                {
                    deserializedInput[p.Position].ToObject(p.ParameterType);

                    if (input == null)
                        return null;

                    return _dataConverter.Deserialize(input, p.ParameterType);
                }).ToArray();
        }

        private async Task<string> SerializeResult(object result)
        {
            if (result is Task task)
            {
                await task;

                if (task.GetType().IsGenericType)
                {
                    result = task.GetType().GetProperty("Result").GetValue(task);
                }
                else
                {
                    result = null;
                }
            }

            var serializedResult = _dataConverter.Serialize(result);

            return serializedResult;
        }
    }
}
