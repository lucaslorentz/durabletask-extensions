using System.Collections.Generic;
using DurableTask.Core;

namespace LLL.DurableTask.Core
{
    public class OrchestrationQueryResult
    {
        public IList<OrchestrationState> Orchestrations { get; set; }
        public long? Count { get; set; }
        public string ContinuationToken { get; set; }
    }
}
