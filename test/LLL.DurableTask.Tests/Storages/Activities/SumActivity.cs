using DurableTask.Core;

namespace LLL.DurableTask.Tests.Storage.Activities
{
    public class SumActivity : TaskActivity<SumActivity.Input, int>
    {
        public const string Name = "Sum";
        public const string Version = "v1";

        protected override int Execute(TaskContext context, Input input)
        {
            return input.LeftValue + input.RightValue;
        }

        public class Input
        {
            public int LeftValue { get; set; }
            public int RightValue { get; set; }
        }
    }
}
