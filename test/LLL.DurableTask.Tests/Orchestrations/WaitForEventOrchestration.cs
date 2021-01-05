using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Orchestrations
{
    public class WaitForEventOrchestration : DistributedTaskOrchestration<object, object, string, object>
    {
        public const string Name = "WaitForEvent";
        public const string Version = "v1";

        private TaskCompletionSource<string> _eventTaskCompletionSource;

        public override async Task<object> RunTask(OrchestrationContext context, object _)
        {
            _eventTaskCompletionSource = new TaskCompletionSource<string>();
            var eventInput = await _eventTaskCompletionSource.Task;
            return eventInput;
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (name == "SetResult")
                _eventTaskCompletionSource?.TrySetResult(input);
        }
    }
}