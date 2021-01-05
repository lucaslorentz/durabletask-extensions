using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker
{
    public abstract class DistributedTaskOrchestration<TResult, TInput> : TaskOrchestration<TResult, TInput>
    {
        public DistributedTaskOrchestration()
        {
            DataConverter = new UntypedJsonDataConverter();
        }

        public override Task<string> Execute(OrchestrationContext context, string input)
        {
            context.MessageDataConverter = new UntypedJsonDataConverter();
            context.ErrorDataConverter = new UntypedJsonDataConverter();

            return base.Execute(context, input);
        }
    }

    public abstract class DistributedTaskOrchestration<TResult, TInput, TEvent, TStatus> : TaskOrchestration<TResult, TInput, TEvent, TStatus>
    {
        public DistributedTaskOrchestration()
        {
            DataConverter = new UntypedJsonDataConverter();
        }

        public override Task<string> Execute(OrchestrationContext context, string input)
        {
            context.MessageDataConverter = new UntypedJsonDataConverter();
            context.ErrorDataConverter = new UntypedJsonDataConverter();

            return base.Execute(context, input);
        }
    }
}
