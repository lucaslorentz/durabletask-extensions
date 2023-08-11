using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Core;

public interface IDistributedOrchestrationService
{
    Task<TaskOrchestrationWorkItem> LockNextTaskOrchestrationWorkItemAsync(
        TimeSpan receiveTimeout,
        INameVersionInfo[] orchestrations,
        CancellationToken cancellationToken);

    Task<TaskActivityWorkItem> LockNextTaskActivityWorkItem(
        TimeSpan receiveTimeout,
        INameVersionInfo[] activities,
        CancellationToken cancellationToken);
}
