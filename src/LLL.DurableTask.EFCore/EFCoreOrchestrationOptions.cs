using System;
using DurableTask.Core.Serializing;

namespace LLL.DurableTask.EFCore
{
    public class EFCoreOrchestrationOptions
    {
        public DataConverter DataConverter { get; set; } = new JsonDataConverter();
        public int TaskOrchestrationDispatcherCount { get; set; } = 1;
        public int TaskActivityDispatcherCount { get; set; } = 1;
        public int MaxConcurrentTaskOrchestrationWorkItems { get; set; } = 20;
        public int MaxConcurrentTaskActivityWorkItems { get; set; } = 10;
        public PollingIntervalOptions PollingInterval { get; } = new PollingIntervalOptions(100, 2, 1000);
        public TimeSpan OrchestrationLockTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan ActivtyLockTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan FetchNewMessagesPollingTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public int DelayInSecondsAfterFailure { get; set; } = 5;

        public class PollingIntervalOptions
        {
            public PollingIntervalOptions(double initial, double factor, double max)
            {
                Initial = initial;
                Factor = factor;
                Max = max;
            }

            public double Initial { get; set; }
            public double Factor { get; set; }
            public double Max { get; set; }
        }
    }
}
