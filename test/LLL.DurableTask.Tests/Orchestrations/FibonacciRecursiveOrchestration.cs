using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Server.Tests.Activities;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Server.Tests.Orchestrations
{
    public class FibonacciRecursiveOrchestration : DistributedTaskOrchestration<FibonacciRecursiveOrchestration.Result, FibonacciRecursiveOrchestration.Input>
    {
        public const string Name = "FibonnaciRecursive";
        public const string Version = "v1";

        public override async Task<Result> RunTask(OrchestrationContext context, Input input)
        {
            if (input.Number <= 1)
            {
                return new Result
                {
                    Value = input.Number
                };
            }

            var fibNMinus1Task = context.CreateSubOrchestrationInstance<Result>(Name, Version, new Input
            {
                Number = input.Number - 1
            });

            var fibNMinus2Task = context.CreateSubOrchestrationInstance<Result>(Name, Version, new Input
            {
                Number = input.Number - 2
            });

            var fibNMinus1 = await fibNMinus1Task;
            var fibNMinus2 = await fibNMinus2Task;

            var sum = await context.ScheduleTask<SumActivity.Result>(SumActivity.Name, SumActivity.Version, new SumActivity.Input
            {
                LeftValue = fibNMinus1.Value,
                RightValue = fibNMinus2.Value
            });

            return new Result
            {
                Value = sum.Value
            };
        }

        public class Input
        {
            public int Number { get; set; }
        }

        public class Result
        {
            public int Value { get; set; }
        }
    }
}
