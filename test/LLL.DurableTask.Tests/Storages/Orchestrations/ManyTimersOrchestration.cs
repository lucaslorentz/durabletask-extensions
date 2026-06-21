using System.Collections.Generic;
using System.Threading.Tasks;
using LLL.DurableTask.Worker;

namespace LLL.DurableTask.Tests.Storage.Orchestrations;

public class ManyTimersOrchestration : OrchestrationBase<object, object>
{
    public const string Name = "ManyTimers";
    public const string Version = "v1";

    public override async Task<object> Execute(object input)
    {
        // Schedule many timers 500ms apart (spanning ~50s) and publish how many have
        // fired so far. The spacing guarantees some fire during the test (so we
        // terminate while the orchestration is actively timing), while the later ones
        // are too far out to fire within the test's polling window — so an empty queue
        // afterwards proves terminating cancelled them rather than them just firing.
        var fired = 0;
        Context.SetStatusProvider(() => fired);

        var timers = new List<Task>();
        for (var i = 1; i <= 100; i++)
        {
            timers.Add(Context.CreateTimer<object>(Context.CurrentUtcDateTime.AddMilliseconds(i * 500), null));
        }

        foreach (var timer in timers)
        {
            await timer;
            fired++;
        }

        return fired;
    }
}
