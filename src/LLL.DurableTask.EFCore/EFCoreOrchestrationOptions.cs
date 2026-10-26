using System;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;
using LLL.DurableTask.EFCore.Polling;

namespace LLL.DurableTask.EFCore;

public class EFCoreOrchestrationOptions
{
    public DataConverter DataConverter { get; set; } = new TypelessJsonDataConverter();
    public int TaskOrchestrationDispatcherCount { get; set; } = 1;
    public int TaskActivityDispatcherCount { get; set; } = 1;
    public int MaxConcurrentTaskOrchestrationWorkItems { get; set; } = 20;
    public int MaxConcurrentTaskActivityWorkItems { get; set; } = 10;
    public PollingIntervalOptions PollingInterval { get; } = new PollingIntervalOptions(100, 2, 1000);
    public TimeSpan OrchestrationLockTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan ActivityLockTimeout { get; set; } = TimeSpan.FromMinutes(1);

    [Obsolete("Misspelled, use ActivityLockTimeout instead.")]
    public TimeSpan ActivtyLockTimeout
    {
        get => ActivityLockTimeout;
        set => ActivityLockTimeout = value;
    }
    public TimeSpan FetchNewMessagesPollingTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public int DelayInSecondsAfterFailure { get; set; } = 5;
    public bool UseDTFxRewind { get; set; } = true;
}
