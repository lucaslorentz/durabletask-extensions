using System.Collections.Generic;

namespace LLL.DurableTask.Server.Api.Models
{
    public class OrchestrationsResult
    {
        public IList<Orchestration> Orchestrations { get; set; }
        public long? Count { get; set; }
        public string ContinuationToken { get; set; }
    }
}
