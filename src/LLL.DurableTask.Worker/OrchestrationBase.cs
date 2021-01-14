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
        public OrchestrationGuidGenerator GuidGenerator { get; }

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

            var parameter = DataConverter.Deserialize<TInput>(input);

            string serializedResult;
            try
            {
                var result = await Execute(parameter);
                serializedResult = DataConverter.Serialize(result);
            }
            catch (Exception e) when (!DUtils.IsFatal(e) && !DUtils.IsExecutionAborting(e))
            {
                var details = DUtils.SerializeCause(e, DataConverter);
                throw new OrchestrationFailureException(e.Message, details);
            }
            return serializedResult;
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
