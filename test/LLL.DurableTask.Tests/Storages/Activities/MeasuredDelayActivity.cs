using System;
using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Tests.Storage.Activities
{
    public class MeasuredDelayActivity : AsyncTaskActivity<int, MeasuredDelayActivity.Output>
    {
        public const string Name = "MeasuredDelay";
        public const string Version = "v1";

        protected override async Task<Output> ExecuteAsync(TaskContext context, int milliseconds)
        {
            var start = DateTime.UtcNow;
            await Task.Delay(milliseconds);
            return new Output
            {
                Start = start,
                End = DateTime.UtcNow
            };
        }

        public class Output
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }
    }
}
