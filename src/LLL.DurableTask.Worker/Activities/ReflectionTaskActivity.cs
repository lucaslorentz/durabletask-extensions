using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DurableTask.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LLL.DurableTask.Worker.Orchestrations
{
    public class ReflectionTaskActivity : TaskActivity
    {
        private readonly MethodInfo _methodInfo;

        public ReflectionTaskActivity(
            object instance,
            MethodInfo methodInfo)
        {
            Instance = instance;
            _methodInfo = methodInfo;
        }

        public object Instance { get; }

        public override string Run(TaskContext context, string input)
        {
            throw new NotImplementedException();
        }

        public override async Task<string> RunAsync(TaskContext context, string input)
        {
            var deserializedInput = JsonConvert.DeserializeObject<JArray>(input);

            var parameters = _methodInfo
                .GetParameters()
                .Select(p => deserializedInput[p.Position].ToObject(p.ParameterType))
                .ToArray();

            var result = _methodInfo.Invoke(Instance, parameters);

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

            var serializedResult = JsonConvert.SerializeObject(result);

            return serializedResult;
        }
    }
}
