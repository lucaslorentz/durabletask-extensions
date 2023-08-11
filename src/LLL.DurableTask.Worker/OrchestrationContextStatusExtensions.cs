using System;

namespace LLL.DurableTask.Worker
{
    public static class OrchestrationContextStatusExtensions
    {
        public static void SetStatusProvider<T>(
            this ExtendedOrchestrationContext context,
            Func<T> statusProvider)
        {
            if (statusProvider == null)
            {
                context.StatusProvider = null;
                return;
            }

            context.StatusProvider = () => context.MessageDataConverter.Serialize(statusProvider());
        }
    }
}
