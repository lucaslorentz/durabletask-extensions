using System;

namespace LLL.DurableTask.EFCore.Entities
{
    public class OrchestrationMessage
    {
        public Guid Id { get; set; }

        public Guid BatchId { get; set; }
        public OrchestrationBatch Batch { get; set; }

        public string ExecutionId { get; set; }

        public DateTime AvailableAt { get; set; }

        /// <summary>
        /// Used to order messages fired at the same moment
        /// </summary>
        public int SequenceNumber { get; set; }

        public string Message { get; set; }
    }
}
