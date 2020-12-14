using System;
using System.Collections.Generic;
using DurableTask.Core;

namespace LLL.DurableTask.Core
{
    public class OrchestrationQuery
    {
        public int Top { get; set; } = 10;
        public string ContinuationToken { get; set; }
        public string InstanceId { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedTimeFrom { get; set; }
        public DateTime? CreatedTimeTo { get; set; }
        public IEnumerable<OrchestrationStatus> RuntimeStatus { get; set; }
    }
}
