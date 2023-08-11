using System;

namespace LLL.DurableTask.EFCore.Entities;

public class Event
{
    public Guid Id { get; set; }

    public string InstanceId { get; set; }

    public string ExecutionId { get; set; }
    // This relationship ensures events are deleted when it's execution is deleted
    public Execution Execution { get; set; }

    public int SequenceNumber { get; set; }

    public string Content { get; set; }
}
