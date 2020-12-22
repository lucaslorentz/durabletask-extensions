using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;

namespace BpmnWorker.Activities
{
    [Activity(Name = "Empty")]
    public class EmptyActivity : DistributedAsyncTaskActivity<object, object>
    {
        protected override Task<object> ExecuteAsync(TaskContext context, object input)
        {
            return Task.FromResult(default(object));
        }
    }
}
