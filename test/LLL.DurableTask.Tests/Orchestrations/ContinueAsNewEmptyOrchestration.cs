using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Orchestrations
{
    public class ContinueAsNewEmptyOrchestration : DistributedTaskOrchestration<int, int>
    {
        public const string Name = "ContinueAsNewEmpty";
        public const string Version = "v1";

        public override Task<int> RunTask(OrchestrationContext context, int input)
        {
            if (input > 0)
            {
                context.ContinueAsNew(input - 1);
            }

            return Task.FromResult(input);
        }
    }
}