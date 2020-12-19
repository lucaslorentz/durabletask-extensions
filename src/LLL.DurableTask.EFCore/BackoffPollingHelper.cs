using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.EFCore
{
    public static class BackoffPollingHelper
    {
        public static async Task<T> PollAsync<T>(
            Func<Task<T>> valueProvider,
            Func<T, bool> shouldAcceptValue,
            TimeSpan timeout,
            double minDelay,
            double factor,
            double maxDelay,
            CancellationToken cancellationToken)
        {
            var value = default(T);

            var stopwatch = Stopwatch.StartNew();

            var delayEnumerator = new ExponentialBackoff(minDelay, factor, maxDelay);
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                value = await valueProvider();

                if (shouldAcceptValue(value)
                    || stopwatch.Elapsed >= timeout)
                    break;

                await Task.Delay((int)delayEnumerator.Next());
            } while (stopwatch.Elapsed < timeout);

            return value;
        }

        class ExponentialBackoff
        {
            private int _count = 0;

            public ExponentialBackoff(double initial, double factor, double max)
            {
                Initial = initial;
                Factor = factor;
                Max = max;
            }

            public double Initial { get; }
            public double Factor { get; }
            public double Max { get; }

            public double Next()
            {
                return Math.Min(Initial * Math.Pow(Factor, _count++), Max);
            }
        }
    }
}
