using System;

namespace LLL.DurableTask.EFCore.Entities
{
    public class ActivityMessage
    {
        public Guid Id { get; set; }

        public string InstanceId { get; set; }
        public Instance Instance { get; set; }

        public string ExecutionId { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Name { get; set; }
        public string Version { get; set; }

        public string Message { get; set; }

        public string Queue { get; set; }
        public DateTime AvailableAt { get; set; }
        public string LockId { get; set; }
    }
}
