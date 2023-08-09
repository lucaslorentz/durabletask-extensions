using System;
using System.Collections.Generic;
using DurableTask.Core.Query;

namespace LLL.DurableTask.Core
{
    public class ExtendedOrchestrationQuery : OrchestrationQuery
    {
        public string NamePrefix { get; set; }
        public DateTime? CompletedTimeFrom { get; set; }
        public DateTime? CompletedTimeTo { get; set; }
        public bool IncludePreviousExecutions { get; set; }
        public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();
    }
}
