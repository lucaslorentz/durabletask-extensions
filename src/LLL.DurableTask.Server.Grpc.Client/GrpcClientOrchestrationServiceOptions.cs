using System;
using DurableTask.Core.Serializing;

namespace LLL.DurableTask.Server.Client
{
    public class GrpcClientOrchestrationServiceOptions
    {
        public Uri BaseAddress { get; set; }
        public DataConverter DataConverter { get; set; } = new JsonDataConverter();
        public int TaskOrchestrationDispatcherCount { get; set; } = 1;
        public int TaskActivityDispatcherCount { get; set; } = 1;
        public int MaxConcurrentTaskOrchestrationWorkItems { get; set; } = 20;
        public int MaxConcurrentTaskActivityWorkItems { get; set; } = 10;
        public int DelayInSecondsAfterFailure { get; set; } = 5;
    }
}
