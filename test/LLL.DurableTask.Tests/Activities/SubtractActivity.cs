using DurableTask.Core;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Activities
{
    public class SubtractActivity : DistributedTaskActivity<SubtractActivity.Input, int>
    {
        public const string Name = "SubtractActivity";
        public const string Version = "v1";

        protected override int Execute(TaskContext context, Input input)
        {
            return input.LeftValue - input.RightValue;
        }

        public class Input
        {
            public int LeftValue { get; set; }
            public int RightValue { get; set; }
        }
    }
}
