using System;

namespace LLL.DurableTask.EFCore.Entities
{
    public class Instance
    {
        public string InstanceId { get; set; }
        public string LastExecutionId { get; set; }
        public Execution LastExecution { get; set; }
        public string Queue { get; set; }
        public DateTime AvailableAt { get; set; }
        public string LockId { get; set; }
    }
}
