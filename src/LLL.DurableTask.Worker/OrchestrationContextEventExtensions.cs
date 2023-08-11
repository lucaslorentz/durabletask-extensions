using System;
using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.Worker
{
    public static class OrchestrationContextEventExtensions
    {
        public static async Task<T> WaitForEventAsync<T>(
            this ExtendedOrchestrationContext context,
            string eventType,
            TimeSpan timeout,
            T defaultValue,
            CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timerTask = context.CreateTimer<object>(context.CurrentUtcDateTime.Add(timeout), null, cts.Token);
            var eventTask = context.WaitForEventAsync<T>(eventType, cts.Token);
            var winningTask = await Task.WhenAny(timerTask, eventTask);
            cts.Cancel();
            return timerTask == winningTask
                ? defaultValue
                : await eventTask;
        }

        public static async Task<T> WaitForEventAsync<T>(
            this ExtendedOrchestrationContext context,
            string eventType,
            CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken)))
            {
                using (context.AddEventListener<T>(eventType, taskCompletionSource.SetResult, taskCompletionSource.SetException))
                {
                    return await taskCompletionSource.Task;
                }
            }
        }

        public static IDisposable AddEventListener<T>(
            this ExtendedOrchestrationContext context,
            string eventType,
            Action<T> handler,
            Action<Exception> exceptionHandler = null)
        {
            return new OrchestrationEventListener(context, (type, input) =>
            {
                if (type == eventType)
                {
                    try
                    {
                        handler(context.MessageDataConverter.Deserialize<T>(input));
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler?.Invoke(ex);
                    }
                }
            });
        }

        public static IDisposable AddEventListener(
            this ExtendedOrchestrationContext context,
            Action<string, string> handler)
        {
            return new OrchestrationEventListener(context, handler);
        }

        private class OrchestrationEventListener : IDisposable
        {
            private readonly ExtendedOrchestrationContext _context;
            private readonly Action<string, string> _handler;

            public OrchestrationEventListener(
                ExtendedOrchestrationContext context,
                Action<string, string> handler)
            {
                _context = context;
                _handler = handler;
                _context.Event += _handler;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                _context.Event -= _handler;
            }
        }
    }
}
