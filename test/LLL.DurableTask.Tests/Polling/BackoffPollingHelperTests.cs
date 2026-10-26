using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using LLL.DurableTask.EFCore.Polling;
using Xunit;

namespace LLL.DurableTask.Tests.Polling;

public class BackoffPollingHelperTests
{
    [Fact]
    public async Task PollAsync_ShouldStopWaitingWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        var stopwatch = Stopwatch.StartNew();

        // The value is never accepted and the delay between polls is far longer than the test
        var poll = BackoffPollingHelper.PollAsync(
            () => Task.FromResult<object>(null),
            x => x is not null,
            TimeSpan.FromMinutes(1),
            new PollingIntervalOptions(30_000, 1, 30_000),
            cts.Token);

        await poll.Invoking(p => p).Should().ThrowAsync<OperationCanceledException>();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
}
