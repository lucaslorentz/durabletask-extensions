using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Tests.Storage.Activities;

namespace LLL.DurableTask.Tests.Storage.Orchestrations;

// Minimal orchestration that schedules exactly one activity. Used by the
// distributed-tracing tests to assert client -> orchestration -> activity spans.
public class SingleActivityOrchestration : TaskOrchestration<int, int>
{
    public const string Name = "SingleActivity";
    public const string Version = "v1";

    public override async Task<int> RunTask(OrchestrationContext context, int input)
    {
        return await context.ScheduleTask<int>(SumActivity.Name, SumActivity.Version, new SumActivity.Input
        {
            LeftValue = input,
            RightValue = 1
        });
    }
}
