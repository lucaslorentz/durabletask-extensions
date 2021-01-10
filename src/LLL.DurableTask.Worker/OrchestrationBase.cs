using System;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Exceptions;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;
using LLL.DurableTask.Worker.Utils;
using DUtils = DurableTask.Core.Common.Utils;

namespace LLL.DurableTask.Worker
{
    public abstract class OrchestrationBase<TResult, TInput> : TaskOrchestration
    {
        public DataConverter DataConverter { get; }
        public OrchestrationContext Context { get; private set; }
        public OrchestrationEventReceiver EventReceiver { get; }
        public OrchestrationGuidGenerator GuidGenerator { get; }

        public OrchestrationBase()
        {
            DataConverter = new TypelessJsonDataConverter();
            EventReceiver = new OrchestrationEventReceiver();
        }

        public override async Task<string> Execute(OrchestrationContext context, string input)
        {
            var parameter = DataConverter.Deserialize<TInput>(input);
            string result;

            try
            {
                context.MessageDataConverter = new TypelessJsonDataConverter();
                context.ErrorDataConverter = new TypelessJsonDataConverter();
                Context = context;
                result = DataConverter.Serialize(await RunTask(parameter));
            }
            catch (Exception e) when (!DUtils.IsFatal(e) && !DUtils.IsExecutionAborting(e))
            {
                var details = DUtils.SerializeCause(e, DataConverter);
                throw new OrchestrationFailureException(e.Message, details);
            }

            return result;
        }

        public override string GetStatus()
        {
            return DataConverter.Serialize(OnGetStatus());
        }

        public override void RaiseEvent(OrchestrationContext context, string name, string input)
        {
            EventReceiver.RaiseEvent(name, input);
        }

        public abstract Task<TResult> RunTask(TInput input);

        public virtual object OnGetStatus()
        {
            return null;
        }
    }
}
