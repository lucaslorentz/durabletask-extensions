using System;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Exceptions;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;
using DUtils = DurableTask.Core.Common.Utils;

namespace LLL.DurableTask.Worker
{
    public abstract class OrchestrationBase<TResult, TInput> : TaskOrchestration
    {
        public DataConverter DataConverter { get; }
        public OrchestrationContext Context { get; private set; }
        public OrchestrationEventReceiver EventReceiver { get; private set; }
        public OrchestrationGuidGenerator GuidGenerator { get; private set; }

        public OrchestrationBase()
        {
            DataConverter = new TypelessJsonDataConverter();
        }

        public sealed override async Task<string> Execute(OrchestrationContext context, string input)
        {
            context.MessageDataConverter = new TypelessJsonDataConverter();
            context.ErrorDataConverter = new TypelessJsonDataConverter();
            Context = context;
            EventReceiver = new OrchestrationEventReceiver(context);
            GuidGenerator = new OrchestrationGuidGenerator(context.OrchestrationInstance.ExecutionId);

            var parameter = DataConverter.Deserialize<TInput>(input);

            var result = await Execute(parameter);
            return DataConverter.Serialize(result);
        }

        public sealed override string GetStatus()
        {
            return DataConverter.Serialize(OnGetStatus());
        }

        public sealed override void RaiseEvent(OrchestrationContext context, string name, string input)
        {
            EventReceiver.RaiseEvent(name, input);
        }

        public abstract Task<TResult> Execute(TInput input);

        public virtual object OnGetStatus()
        {
            return null;
        }
    }
}
