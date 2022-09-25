using System;

namespace LLL.DurableTask.EFCore.Entities
{
    public class Instance
    {
        public string InstanceId { get; set; }
        public string LastExecutionId { get; set; }
        // This relationship ensures instance is deleted when last execution is deleted
        public Execution LastExecution { get; set; }
        public string LastQueue { get; set; }
        public DateTime LockedUntil { get; set; }
        public string LockId { get; set; }
    }
}
