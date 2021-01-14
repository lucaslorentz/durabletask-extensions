using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker
{
    public class OrchestrationEventReceiver
    {
        private readonly DataConverter _dataConverter;
        private readonly OrchestrationContext _orchestrationContext;

        public event Action<string, string> Event;

        public OrchestrationEventReceiver(OrchestrationContext orchestrationContext)
            : this(orchestrationContext, new TypelessJsonDataConverter())
        {
        }

        public OrchestrationEventReceiver(
            OrchestrationContext orchestrationContext,
            DataConverter dataConverter)
        {
            _dataConverter = dataConverter;
            _orchestrationContext = orchestrationContext;
        }

        public void RaiseEvent(string eventType, string input)
        {
            Event?.Invoke(eventType, input);
        }

        public async Task<T> WaitForEventAsync<T>(
            string eventType,
            TimeSpan timeout,
            T defaultValue,
            CancellationToken cancellationToken = default)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var timerTask = _orchestrationContext.CreateTimer<object>(_orchestrationContext.CurrentUtcDateTime.Add(timeout), null, cts.Token);
                var eventTask = WaitForEventAsync<T>(eventType, cts.Token);
                var winningTask = await Task.WhenAny(timerTask, eventTask);
                cts.Cancel();
                return timerTask == winningTask
                    ? defaultValue
                    : await eventTask;
            }
        }

        public async Task<T> WaitForEventAsync<T>(string eventType, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken)))
            {
                using (AddListener<T>(eventType, taskCompletionSource.SetResult, taskCompletionSource.SetException))
                {
                    return await taskCompletionSource.Task;
                }
            }
        }

        public OrchestrationEventListener AddListener<T>(
            string eventType,
            Action<T> handler,
            Action<Exception> exceptionHandler = null)
        {
            return new OrchestrationEventListener(this, (type, input) =>
            {
                if (type == eventType)
                {
                    try
                    {
                        handler(_dataConverter.Deserialize<T>(input));
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler?.Invoke(ex);
                    }
                }
            });
        }

        public OrchestrationEventListener AddListener(Action<string, string> handler)
        {
            return new OrchestrationEventListener(this, handler);
        }
    }
}
