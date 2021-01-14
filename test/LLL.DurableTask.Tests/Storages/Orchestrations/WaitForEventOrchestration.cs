using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Tests.Storage.Orchestrations
{
    public class WaitForEventOrchestration : TaskOrchestration<object, object, object, object>
    {
        public const string Name = "WaitForEvent";
        public const string Version = "v1";

        private TaskCompletionSource<object> _eventTaskCompletionSource;

        public override async Task<object> RunTask(OrchestrationContext context, object _)
        {
            _eventTaskCompletionSource = new TaskCompletionSource<object>();
            var eventInput = await _eventTaskCompletionSource.Task;
            return eventInput;
        }

        public override void OnEvent(OrchestrationContext context, string name, object input)
        {
            if (name == "SetResult")
                _eventTaskCompletionSource?.TrySetResult(input);
        }
    }
}