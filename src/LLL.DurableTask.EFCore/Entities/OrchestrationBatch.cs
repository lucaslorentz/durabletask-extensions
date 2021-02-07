using System;

namespace LLL.DurableTask.EFCore.Entities
{
    public class OrchestrationBatch
    {
        public Guid Id { get; set; }

        public string InstanceId { get; set; }
        public Instance Instance { get; set; }

        public string Queue { get; set; }

        public DateTime AvailableAt { get; set; }

        public DateTime LockedUntil { get; set; }

        public string LockId { get; set; }
    }
}
