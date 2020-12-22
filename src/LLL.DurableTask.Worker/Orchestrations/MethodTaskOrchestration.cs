using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DurableTask.Core;
using Newtonsoft.Json;

namespace LLL.DurableTask.Worker.Orchestrations
{
    public class MethodTaskOrchestration : TaskOrchestration
    {
        private readonly MethodInfo _methodInfo;

        public MethodTaskOrchestration(
            object instance,
            MethodInfo methodInfo)
        {
            Instance = instance;
            _methodInfo = methodInfo;
        }

        public object Instance { get; }

        public override async Task<string> Execute(OrchestrationContext context, string input)
        {
            var parameters = _methodInfo
                .GetParameters()
                .Select(p =>
                {
                    if (p.ParameterType == typeof(OrchestrationContext))
                        return context;

                    if (input == null)
                        return null;

                    return JsonConvert.DeserializeObject(input, p.ParameterType);
                }).ToArray();

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

        public override string GetStatus()
        {
            return null;
        }

        public override void RaiseEvent(OrchestrationContext context, string name, string input)
        {
        }
    }
}
