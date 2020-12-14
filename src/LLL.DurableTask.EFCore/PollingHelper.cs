using System;
using System.Threading;
using System.Threading.Tasks;

namespace LLL.DurableTask.EFCore
{
    public static class PollingHelper
    {
        public static async Task<T> PollAsync<T>(
            Func<Task<T>> func,
            Func<T, bool> release,
            TimeSpan interval,
            CancellationToken cancellationToken)
        {
            T value;

            do
            {
                value = await func();

                if (release(value))
                    break;

                await Task.Delay(interval);
            } while (!cancellationToken.IsCancellationRequested);

            return value;
        }
    }
}
