using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker
{
    public abstract class OrchestrationBase<TResult, TInput> : TaskOrchestration
    {
        public ExtendedOrchestrationContext Context { get; private set; }

        public sealed override async Task<string> Execute(OrchestrationContext context, string input)
        {
            context.MessageDataConverter = new TypelessJsonDataConverter();
            context.ErrorDataConverter = new TypelessJsonDataConverter();
            Context = new ExtendedOrchestrationContext(context);

            var parameter = context.MessageDataConverter.Deserialize<TInput>(input);

            var result = await Execute(parameter);
            return context.MessageDataConverter.Serialize(result);
        }

        public sealed override string GetStatus()
        {
            if (Context.StatusProvider != null)
                return Context.StatusProvider();

            return Context.MessageDataConverter.Serialize(OnGetStatus());
        }

        public sealed override void RaiseEvent(OrchestrationContext context, string name, string input)
        {
            Context.RaiseEvent(name, input);
        }

        public abstract Task<TResult> Execute(TInput input);

        public virtual object OnGetStatus()
        {
            return null;
        }
    }
}
