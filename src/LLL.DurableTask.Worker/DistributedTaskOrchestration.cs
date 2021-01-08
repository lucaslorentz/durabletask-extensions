using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker
{
    public abstract class DistributedTaskOrchestration<TResult, TInput> : TaskOrchestration<TResult, TInput, string, string>
    {
    }

    public abstract class DistributedTaskOrchestration<TResult, TInput, TEvent, TStatus> : TaskOrchestration<TResult, TInput, TEvent, TStatus>
    {
        public DistributedTaskOrchestration()
        {
            DataConverter = new TypelessJsonDataConverter();
        }

        public override Task<string> Execute(OrchestrationContext context, string input)
        {
            context.MessageDataConverter = new TypelessJsonDataConverter();
            context.ErrorDataConverter = new TypelessJsonDataConverter();

            return base.Execute(context, input);
        }
    }
}
