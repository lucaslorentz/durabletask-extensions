using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Orchestrations
{
    public class EmptyOrchestration : DistributedTaskOrchestration<object, object>
    {
        public const string Name = "Empty";
        public const string Version = "v1";

        public override Task<object> RunTask(OrchestrationContext context, object input)
        {
            return Task.FromResult(input);
        }
    }
}