using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Orchestrations
{
    public class ParentOrchestration : DistributedTaskOrchestration<int, int>
    {
        public const string Name = "ParentOrchestration";
        public const string Version = "v1";

        public override async Task<int> RunTask(OrchestrationContext context, int input)
        {
            var subOrchestrationOutput = await context.CreateSubOrchestrationInstance<int>(EmptyOrchestration.Name, EmptyOrchestration.Version, input);

            return subOrchestrationOutput;
        }
    }
}
