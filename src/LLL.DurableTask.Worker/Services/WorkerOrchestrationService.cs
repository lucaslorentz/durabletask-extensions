using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.Worker.Services;

public class WorkerOrchestrationService : IOrchestrationService
{
    private readonly IOrchestrationService _innerOrchestrationService;
    private readonly IDistributedOrchestrationService _innerDistributedOrchestrationService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly INameVersionInfo[] _orchestrations;
    private readonly INameVersionInfo[] _activities;
    private readonly bool _hasAllOrchestrations;
    private readonly bool _hasAllActivities;

    public static ConcurrentDictionary<string, IServiceScope> OrchestrationsServiceScopes { get; } = new ConcurrentDictionary<string, IServiceScope>();

    public int TaskOrchestrationDispatcherCount => _orchestrations.Length == 0
        ? 0
        : _innerOrchestrationService.TaskOrchestrationDispatcherCount;
    public int TaskActivityDispatcherCount => _activities.Length == 0
        ? 0
        : _innerOrchestrationService.TaskActivityDispatcherCount;

    public WorkerOrchestrationService(
        IOrchestrationService innerOrchestrationService,
        IDistributedOrchestrationService innerDistributedOrchestrationService,
        IServiceScopeFactory serviceScopeFactory,
        IEnumerable<ObjectCreator<TaskOrchestration>> orchestrations,
        IEnumerable<ObjectCreator<TaskActivity>> activities,
        bool hasAllOrchestrations,
        bool hasAllActivities)
    {
        _innerOrchestrationService = innerOrchestrationService;
        _innerDistributedOrchestrationService = innerDistributedOrchestrationService;
        _serviceScopeFactory = serviceScopeFactory;
        _orchestrations = orchestrations.OfType<INameVersionInfo>().ToArray();
        _activities = activities.OfType<INameVersionInfo>().ToArray();
        _hasAllOrchestrations = hasAllOrchestrations;
        _hasAllActivities = hasAllActivities;
    }

    public async Task<TaskOrchestrationWorkItem> LockNextTaskOrchestrationWorkItemAsync(TimeSpan receiveTimeout, CancellationToken cancellationToken)
    {
        var workItem = await (_hasAllOrchestrations
            ? _innerOrchestrationService
                .LockNextTaskOrchestrationWorkItemAsync(receiveTimeout, cancellationToken)
            : (_innerDistributedOrchestrationService ?? throw DistributedWorkersNotSupported())
                .LockNextTaskOrchestrationWorkItemAsync(receiveTimeout, _orchestrations, cancellationToken)
        );

        if (workItem is not null)
        {
            OrchestrationsServiceScopes.TryAdd(workItem.InstanceId, _serviceScopeFactory.CreateScope());
        }

        return workItem;
    }

    public Task AbandonTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
    {
        if (OrchestrationsServiceScopes.TryRemove(workItem.InstanceId, out var serviceScope))
            serviceScope.Dispose();

        return _innerOrchestrationService.AbandonTaskOrchestrationWorkItemAsync(workItem);
    }

    public Task ReleaseTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
    {
        if (OrchestrationsServiceScopes.TryRemove(workItem.InstanceId, out var serviceScope))
            serviceScope.Dispose();

        return _innerOrchestrationService.ReleaseTaskOrchestrationWorkItemAsync(workItem);
    }

    public int MaxConcurrentTaskOrchestrationWorkItems => _innerOrchestrationService.MaxConcurrentTaskOrchestrationWorkItems;

    public BehaviorOnContinueAsNew EventBehaviourForContinueAsNew => _innerOrchestrationService.EventBehaviourForContinueAsNew;

    public int MaxConcurrentTaskActivityWorkItems => _innerOrchestrationService.MaxConcurrentTaskActivityWorkItems;

    public Task AbandonTaskActivityWorkItemAsync(TaskActivityWorkItem workItem)
    {
        return _innerOrchestrationService.AbandonTaskActivityWorkItemAsync(workItem);
    }

    public Task CompleteTaskActivityWorkItemAsync(TaskActivityWorkItem workItem, TaskMessage responseMessage)
    {
        return _innerOrchestrationService.CompleteTaskActivityWorkItemAsync(workItem, responseMessage);
    }

    public Task CompleteTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem, OrchestrationRuntimeState newOrchestrationRuntimeState, IList<TaskMessage> outboundMessages, IList<TaskMessage> orchestratorMessages, IList<TaskMessage> timerMessages, TaskMessage continuedAsNewMessage, OrchestrationState orchestrationState)
    {
        return _innerOrchestrationService.CompleteTaskOrchestrationWorkItemAsync(workItem, newOrchestrationRuntimeState, outboundMessages, orchestratorMessages, timerMessages, continuedAsNewMessage, orchestrationState);
    }

    public Task CreateAsync()
    {
        return _innerOrchestrationService.CreateAsync();
    }

    public Task CreateAsync(bool recreateInstanceStore)
    {
        return _innerOrchestrationService.CreateAsync(recreateInstanceStore);
    }

    public Task CreateIfNotExistsAsync()
    {
        return _innerOrchestrationService.CreateIfNotExistsAsync();
    }

    public Task DeleteAsync()
    {
        return _innerOrchestrationService.DeleteAsync();
    }

    public Task DeleteAsync(bool deleteInstanceStore)
    {
        return _innerOrchestrationService.DeleteAsync(deleteInstanceStore);
    }

    public int GetDelayInSecondsAfterOnFetchException(Exception exception)
    {
        return _innerOrchestrationService.GetDelayInSecondsAfterOnFetchException(exception);
    }

    public int GetDelayInSecondsAfterOnProcessException(Exception exception)
    {
        return _innerOrchestrationService.GetDelayInSecondsAfterOnProcessException(exception);
    }

    public bool IsMaxMessageCountExceeded(int currentMessageCount, OrchestrationRuntimeState runtimeState)
    {
        return _innerOrchestrationService.IsMaxMessageCountExceeded(currentMessageCount, runtimeState);
    }

    public async Task<TaskActivityWorkItem> LockNextTaskActivityWorkItem(TimeSpan receiveTimeout, CancellationToken cancellationToken)
    {
        return await (_hasAllActivities
            ? _innerOrchestrationService
                .LockNextTaskActivityWorkItem(receiveTimeout, cancellationToken)
            : (_innerDistributedOrchestrationService ?? throw DistributedWorkersNotSupported())
                .LockNextTaskActivityWorkItem(receiveTimeout, _activities, cancellationToken)
        );
    }

    public Task<TaskActivityWorkItem> RenewTaskActivityWorkItemLockAsync(TaskActivityWorkItem workItem)
    {
        return _innerOrchestrationService.RenewTaskActivityWorkItemLockAsync(workItem);
    }

    public Task RenewTaskOrchestrationWorkItemLockAsync(TaskOrchestrationWorkItem workItem)
    {
        return _innerOrchestrationService.RenewTaskOrchestrationWorkItemLockAsync(workItem);
    }

    public Task StartAsync()
    {
        return _innerOrchestrationService.StartAsync();
    }

    public Task StopAsync()
    {
        return _innerOrchestrationService.StopAsync();
    }

    public Task StopAsync(bool isForced)
    {
        return _innerOrchestrationService.StopAsync(isForced);
    }

    private Exception DistributedWorkersNotSupported()
    {
        return new NotSupportedException("Distributed workers is not supported by storage implementation");
    }
}
