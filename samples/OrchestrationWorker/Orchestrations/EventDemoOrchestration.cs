using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using static OrchestrationWorker.Orchestrations.EventDemoOrchestration;

namespace OrchestrationWorker.Orchestrations;

[Orchestration(Name = "EventDemo", Version = "v1")]
public class EventDemoOrchestration : OrchestrationBase<EventDemoResult, EventDemoInput>
{
    public override async Task<EventDemoResult> Execute(EventDemoInput input)
    {
        var correlationId = string.IsNullOrWhiteSpace(input?.CorrelationId)
            ? Context.NewGuid().ToString("N")
            : input.CorrelationId;

        var approval = await Context.WaitForEventAsync<ApprovalEvent>("ApprovalRequested");
        var comment = await Context.WaitForEventAsync<CommentEvent>("AddComment");

        return new EventDemoResult
        {
            CorrelationId = correlationId,
            Approved = approval.Approved,
            ApprovedBy = approval.ApprovedBy,
            ApprovalReason = approval.Reason,
            Comment = comment.Text
        };
    }

    public class EventDemoInput
    {
        public string CorrelationId { get; set; }
    }

    public class EventDemoResult
    {
        public string CorrelationId { get; set; }
        public bool Approved { get; set; }
        public string ApprovedBy { get; set; }
        public string ApprovalReason { get; set; }
        public string Comment { get; set; }
    }

    public class ApprovalEvent
    {
        public bool Approved { get; set; }
        public string ApprovedBy { get; set; }
        public string Reason { get; set; }
    }

    public class CommentEvent
    {
        public string Text { get; set; }
    }
}
