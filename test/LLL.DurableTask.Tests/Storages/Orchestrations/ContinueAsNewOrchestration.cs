using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Tests.Storage.Activities;

namespace LLL.DurableTask.Tests.Storage.Orchestrations;

public class ContinueAsNewOrchestration : TaskOrchestration<int, int>
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
