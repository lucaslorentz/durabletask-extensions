using System;

namespace LLL.DurableTask.Worker
{
    public class OrchestrationEventListener : IDisposable
    {
        private readonly OrchestrationEventReceiver _eventReceiver;
        private readonly Action<string, string> _handler;

        public OrchestrationEventListener(
            OrchestrationEventReceiver eventReceiver,
            Action<string, string> handler)
        {
            _eventReceiver = eventReceiver;
            _handler = handler;
            _eventReceiver.Event += _handler;
        }

        public void Dispose()
        {
            _eventReceiver.Event -= _handler;
        }
    }
}
