using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;

namespace BpmnWorker.Activities
{
    [Activity(Name = "Empty")]
    public class EmptyActivity : ActivityBase<object, object>
    {
        protected override object Execute(TaskContext context, object input)
        {
            return base.Execute(context, null);
        }
    }
}
