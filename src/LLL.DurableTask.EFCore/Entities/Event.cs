using System;

namespace LLL.DurableTask.EFCore.Entities
{
    public class Event
    {
        public Guid Id { get; set; }

        public string InstanceId { get; set; }

        public string ExecutionId { get; set; }

        public Execution Execution { get; set; }

        public int SequenceNumber { get; set; }

        public string Content { get; set; }
    }
}
