using System.Collections.Generic;

namespace LLL.DurableTask.Server.Api.Models
{
    public class OrchestrationsResponse
    {
        public IList<Orchestration> Orchestrations { get; set; }
        public long? Count { get; set; }
        public string ContinuationToken { get; set; }
    }
}
