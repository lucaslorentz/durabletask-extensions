using System;

namespace LLL.DurableTask.EFCore.Entities;

public class ActivityMessage
{
    public Guid Id { get; set; }

    public string InstanceId { get; set; }
    // This relationship ensures messages are deleted when instance is deleted
    public Instance Instance { get; set; }

    public string Queue { get; set; }
    public string ReplyQueue { get; set; }

    public string Message { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime LockedUntil { get; set; }
    public string LockId { get; set; }
}
