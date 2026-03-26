using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using static OrchestrationWorker.Orchestrations.RewindDemoOrchestration;

namespace OrchestrationWorker.Orchestrations;

[Orchestration(Name = "RewindDemo", Version = "v1")]
public class RewindDemoOrchestration : OrchestrationBase<RewindDemoResult, RewindDemoInput>
{
    public override async Task<RewindDemoResult> Execute(RewindDemoInput input)
    {
        var instanceId = Context.OrchestrationInstance.InstanceId;

        if (Context.IsReplaying) // Clear the input to avoid the same failure happening again, maybe don't drive a sports car ;)
        {
            input.RequestedCarType = "";
        }

        var bookCarResult = await Context.ScheduleTask<BookItemResult>("BookCar", "v1", new
        {
            RequestedCarType = input.RequestedCarType
        });

        return new RewindDemoResult
        {
            InstanceId = instanceId
        };
    }

    public class RewindDemoInput
    {
        public string RequestedCarType { get; set; }
    }

    public class BookItemResult
    {
        public Guid BookingId { get; set; }
    }

    public class RewindDemoResult
    {
        public string InstanceId { get; set; }
    }
}
