using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Tests.Storage.Activities;

namespace LLL.DurableTask.Tests.Storage.Orchestrations;

public class ParallelTasksOrchestration : TaskOrchestration<object, int>
{
    public const string Name = "ParallelTasks";
    public const string Version = "v1";

    public override async Task<object> RunTask(OrchestrationContext context, int numberOfTasks)
    {
        var tasks = Enumerable.Range(0, numberOfTasks).Select(_ =>
            context.ScheduleTask<MeasuredDelayActivity.Output>(
                MeasuredDelayActivity.Name,
                MeasuredDelayActivity.Version,
                2000)
        );

        var measurements = await Task.WhenAll(tasks);

        var modifications = measurements.Select(m => new { Moment = m.Start, Change = 1 })
            .Concat(measurements.Select(m => new { Moment = m.End, Change = -1 }))
            .OrderBy(m => m.Moment)
            .ToArray();

        var parallelTasks = 0;
        var degreeOfParallelism = 0;
        foreach (var modification in modifications)
        {
            parallelTasks += modification.Change;
            if (parallelTasks > degreeOfParallelism)
                degreeOfParallelism = parallelTasks;
        }

        return degreeOfParallelism;
    }
}
