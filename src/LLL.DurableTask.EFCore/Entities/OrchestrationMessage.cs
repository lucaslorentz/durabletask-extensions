using System;

namespace LLL.DurableTask.EFCore.Entities
{
    public class OrchestrationMessage
    {
        public Guid Id { get; set; }

        public string InstanceId { get; set; }
        // This relationship ensures messages are deleted when instance is deleted
        public Instance Instance { get; set; }

        public string ExecutionId { get; set; }

        public string Queue { get; set; }

        public DateTime AvailableAt { get; set; }

        // Used to order messages fired at the same moment
        public int SequenceNumber { get; set; }

        public string Message { get; set; }
    }
}
