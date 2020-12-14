using DurableTask.Core;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Server.Tests.Activities
{
    public class SumActivity : DistributedTaskActivity<SumActivity.Input, SumActivity.Result>
    {
        public const string Name = "Sum";
        public const string Version = "v1";

        protected override Result Execute(TaskContext context, Input input)
        {
            return new Result
            {
                Value = input.LeftValue + input.RightValue
            };
        }

        public class Input
        {
            public int LeftValue { get; set; }
            public int RightValue { get; set; }
        }

        public class Result
        {
            public int Value { get; set; }
        }
    }
}
