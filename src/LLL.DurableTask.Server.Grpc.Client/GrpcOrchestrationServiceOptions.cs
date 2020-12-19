using System;

namespace LLL.DurableTask.Server.Client
{
    public class GrpcOrchestrationServiceOptions
    {
        public Uri BaseAddress { get; set; }
        public int TaskOrchestrationDispatcherCount { get; set; } = 1;
        public int TaskActivityDispatcherCount { get; set; } = 1;
        public int MaxConcurrentTaskOrchestrationWorkItems { get; set; } = 10;
        public int MaxConcurrentTaskActivityWorkItems { get; set; } = 5;
    }
}
