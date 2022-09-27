using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;

namespace LLL.DurableTask.EFCore.Polling
{
    public static class BackoffPollingHelper
    {
        public static async Task<T> PollAsync<T>(
            Func<Task<T>> valueProvider,
            Func<T, bool> shouldAcceptValue,
            TimeSpan timeout,
            PollingIntervalOptions interval,
            CancellationToken cancellationToken,
            Func<CancellationToken, Task> waitFunction = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string fileName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            T value;

            var stopwatch = Stopwatch.StartNew();
            var count = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var waitTask = (waitFunction ?? WaitUntilCancellation)(waitCts.Token);

                try
                {
                    value = await valueProvider();

                    if (shouldAcceptValue(value)
                        || stopwatch.Elapsed >= timeout)
                        break;

                    waitCts.CancelAfter(CalculateDelay(interval, count++));

                    try
                    {
                        await waitTask;
                    }
                    catch (OperationCanceledException) { }
                }
                finally
                {
                    waitCts.Cancel();
                }
            } while (stopwatch.Elapsed < timeout);

            return value;
        }

        private static int CalculateDelay(PollingIntervalOptions interval, int count)
        {
            return (int)Math.Min(interval.Initial * Math.Pow(interval.Factor, count), interval.Max);
        }

        public static async Task WaitUntilCancellation(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        }

        public static Func<CancellationToken, Task> CreateNatsWaitUntilSignal(IAsyncSubscription subscription, HashSet<string> subjects)
        {
            return async (cancellationToken) =>
            {
                var tcs = new TaskCompletionSource();
                void OnMessage(object o, MsgHandlerEventArgs e)
                {
                    if (subjects.Contains(e.Message.Subject) || subjects.Any(s => e.Message.Subject.StartsWith(s)))
                        tcs.TrySetResult();
                };
                subscription.MessageHandler += OnMessage;
                using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
                try
                {
                    await tcs.Task;
                }
                finally
                {
                    subscription.MessageHandler -= OnMessage;
                }
            };
        }
    }
}
