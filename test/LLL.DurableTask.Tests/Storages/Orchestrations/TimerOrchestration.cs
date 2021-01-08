using System;
using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Tests.Storage.Orchestrations
{
    public class TimerOrchestration : TaskOrchestration<object, object>
    {
        public const string Name = "Timer";
        public const string Version = "v1";

        public override async Task<object> RunTask(OrchestrationContext context, object input)
        {
            var output = await context.CreateTimer<object>(DateTime.UtcNow.AddSeconds(2), input);
            return output;
        }
    }
}