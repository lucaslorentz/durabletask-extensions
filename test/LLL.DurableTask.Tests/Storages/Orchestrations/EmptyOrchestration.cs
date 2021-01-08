using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Tests.Storage.Orchestrations
{
    public class EmptyOrchestration : TaskOrchestration<object, object>
    {
        public const string Name = "Empty";
        public const string Version = "v1";

        public override Task<object> RunTask(OrchestrationContext context, object input)
        {
            return Task.FromResult(input);
        }
    }
}