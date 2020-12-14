using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;

namespace BpmnWorker.Activities
{
    public class EmptyActivity : DistributedAsyncTaskActivity<object, object>
    {
        protected override Task<object> ExecuteAsync(TaskContext context, object input)
        {
            return Task.FromResult(default(object));
        }
    }
}
