using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;

namespace BpmnWorker.Activities;

[Activity(Name = "Empty")]
public class EmptyActivity : ActivityBase<object, object>
{
    public override Task<object> ExecuteAsync(object input)
    {
        return Task.FromResult(default(object));
    }
}
