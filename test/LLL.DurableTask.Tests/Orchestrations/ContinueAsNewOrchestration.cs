using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Tests.Activities;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Orchestrations
{
    public class ContinueAsNewOrchestration : DistributedTaskOrchestration<int, int>
    {
        public const string Name = "ContinueAsNew";
        public const string Version = "v1";

        public override async Task<int> RunTask(OrchestrationContext context, int input)
        {
            if (input > 0)
            {
                var nextIteration = await context.ScheduleTask<int>(SubtractActivity.Name, SubtractActivity.Version, new SubtractActivity.Input
                {
                    LeftValue = input,
                    RightValue = 1
                });

                context.ContinueAsNew(nextIteration);
            }

            return input;
        }
    }
}