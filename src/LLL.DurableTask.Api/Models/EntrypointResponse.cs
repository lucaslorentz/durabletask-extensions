using System.Collections.Generic;
using LLL.DurableTask.Core;

namespace LLL.DurableTask.Api.Models
{
    public class EntrypointResponse
    {
        public Dictionary<string, EndpointInfo> Endpoints { get; set; } = new Dictionary<string, EndpointInfo>();
        public OrchestrationFeature[] Features { get; set; }
    }
}
