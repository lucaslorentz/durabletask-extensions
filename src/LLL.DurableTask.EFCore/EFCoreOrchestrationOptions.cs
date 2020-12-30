using System;
using DurableTask.Core.Serializing;
using LLL.DurableTask.EFCore.Polling;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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
        public TimeSpan FetchNewMessagesPollingTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public int DelayInSecondsAfterFailure { get; set; } = 5;
        
        public DataConverter RewindDataConverter { get; set; } = new JsonDataConverter(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() }
        });
    }
}
