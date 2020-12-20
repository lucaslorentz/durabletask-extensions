using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.EFCore.Polling
{
    public static class BackoffPollingHelper
    {
        public static async Task<T> PollAsync<T>(
            Func<Task<T>> valueProvider,
            Func<T, bool> shouldAcceptValue,
            TimeSpan timeout,
            PollingIntervalOptions interval,
            CancellationToken cancellationToken)
        {
            var value = default(T);

            var stopwatch = Stopwatch.StartNew();

            var count = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                value = await valueProvider();

                if (shouldAcceptValue(value)
                    || stopwatch.Elapsed >= timeout)
                    break;

                await Task.Delay(CalculateDelay(interval, count++));
            } while (stopwatch.Elapsed < timeout);

            return value;
        }

        private static int CalculateDelay(PollingIntervalOptions interval, int count)
        {
            return (int)Math.Min(interval.Initial * Math.Pow(interval.Factor, count), interval.Max);
        }
    }
}
