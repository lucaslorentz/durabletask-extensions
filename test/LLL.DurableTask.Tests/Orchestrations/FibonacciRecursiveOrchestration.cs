using System;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Tests.Activities;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Orchestrations
{
    public class FibonacciRecursiveOrchestration : DistributedTaskOrchestration<int, int>
    {
        public const string Name = "FibonnaciRecursive";
        public const string Version = "v1";

        public override async Task<int> RunTask(OrchestrationContext context, int input)
        {
            if (input <= 1)
            {
                return input;
            }

            var fibNMinus1Task = new Func<Task<int>>(async () =>
            {
                var inputMinus1 = await context.ScheduleTask<int>(SubtractActivity.Name, SubtractActivity.Version, new SubtractActivity.Input
                {
                    LeftValue = input,
                    RightValue = 1
                });

                return await context.CreateSubOrchestrationInstance<int>(Name, Version, inputMinus1);
            })();

            var fibNMinus2Task = new Func<Task<int>>(async () =>
            {
                var inputMinus2 = await context.ScheduleTask<int>(SubtractActivity.Name, SubtractActivity.Version, new SubtractActivity.Input
                {
                    LeftValue = input,
                    RightValue = 2
                });

                return await context.CreateSubOrchestrationInstance<int>(Name, Version, inputMinus2);
            })();

            var fibNMinus1 = await fibNMinus1Task;
            var fibNMinus2 = await fibNMinus2Task;

            return await context.ScheduleTask<int>(SumActivity.Name, SumActivity.Version, new SumActivity.Input
            {
                LeftValue = fibNMinus1,
                RightValue = fibNMinus2
            });
        }
    }
}
