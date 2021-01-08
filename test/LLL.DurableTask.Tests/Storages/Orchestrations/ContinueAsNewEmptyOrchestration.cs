using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Tests.Storage.Orchestrations
{
    public class ContinueAsNewEmptyOrchestration : TaskOrchestration<int, int>
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