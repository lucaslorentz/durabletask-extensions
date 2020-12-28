using System;
using System.Collections.Generic;
using DurableTask.Core;

namespace LLL.DurableTask.EFCore.Entities
{
    public class Execution
    {
        public string ExecutionId { get; set; }
        
        public string InstanceId { get; set; }

        public string Name { get; set; }
        public string Version { get; set; }

        public DateTime CreatedTime { get; set; }
        public DateTime CompletedTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }

        public long CompressedSize { get; set; }
        public long Size { get; set; }

        public OrchestrationStatus Status { get; set; }

        public string CustomStatus { get; set; }

        public string ParentInstance { get; set; }

        public HashSet<Tag> Tags { get; } = new HashSet<Tag>();

        public string Input { get; set; }

        public string Output { get; set; }
    }
}
