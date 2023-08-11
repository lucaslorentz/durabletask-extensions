using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Worker.Utils;

namespace LLL.DurableTask.Worker;

public class ExtendedOrchestrationContext
{
    private static readonly Guid _orchestrationGuidNamespace = new("93810b2d-3411-4fc0-b51b-47f2233dac7a");
    private int _count = 0;

    private readonly OrchestrationContext _baseContext;

    public ExtendedOrchestrationContext(OrchestrationContext baseContext)
    {
        _baseContext = baseContext;
    }

    public OrchestrationContext BaseContext => _baseContext;

    /// <inheritdoc cref="OrchestrationContext.ContinueAsNew" />
    public void ContinueAsNew(object input)
    {
        BaseContext.ContinueAsNew(input);
    }

    /// <inheritdoc cref="OrchestrationContext.ContinueAsNew" />
    public void ContinueAsNew(string newVersion, object input)
    {
        BaseContext.ContinueAsNew(newVersion, input);
    }

    /// <inheritdoc cref="OrchestrationContext.CreateSubOrchestrationInstance" />
    public Task<T> CreateSubOrchestrationInstance<T>(string name, string version, object input)
    {
        return BaseContext.CreateSubOrchestrationInstance<T>(name, version, input);
    }

    /// <inheritdoc cref="OrchestrationContext.CreateSubOrchestrationInstance" />
    public Task<T> CreateSubOrchestrationInstance<T>(string name, string version, string instanceId, object input)
    {
        return BaseContext.CreateSubOrchestrationInstance<T>(name, version, instanceId, input);
    }

    /// <inheritdoc cref="OrchestrationContext.CreateSubOrchestrationInstance" />
    public Task<T> CreateSubOrchestrationInstance<T>(string name, string version, string instanceId, object input, IDictionary<string, string> tags)
    {
        return BaseContext.CreateSubOrchestrationInstance<T>(name, version, instanceId, input, tags);
    }

    /// <inheritdoc cref="OrchestrationContext.CreateTimer" />
    public Task<T> CreateTimer<T>(DateTime fireAt, T state)
    {
        return BaseContext.CreateTimer(fireAt, state); ;
    }

    /// <inheritdoc cref="OrchestrationContext.CreateTimer" />
    public Task<T> CreateTimer<T>(DateTime fireAt, T state, CancellationToken cancelToken)
    {
        return BaseContext.CreateTimer(fireAt, state, cancelToken);
    }

    /// <inheritdoc cref="OrchestrationContext.ScheduleTask" />
    public Task<TResult> ScheduleTask<TResult>(string name, string version, params object[] parameters)
    {
        return BaseContext.ScheduleTask<TResult>(name, version, parameters);
    }

    /// <inheritdoc cref="OrchestrationContext.SendEvent" />
    public void SendEvent(OrchestrationInstance orchestrationInstance, string eventName, object eventData)
    {
        BaseContext.SendEvent(orchestrationInstance, eventName, eventData);
    }

    /// <inheritdoc cref="OrchestrationContext.CurrentUtcDateTime" />
    public virtual DateTime CurrentUtcDateTime => BaseContext.CurrentUtcDateTime;

    /// <inheritdoc cref="OrchestrationContext.IsReplaying" />
    public bool IsReplaying => BaseContext.IsReplaying;

    /// <inheritdoc cref="OrchestrationContext.MessageDataConverter" />
    public JsonDataConverter MessageDataConverter => _baseContext.MessageDataConverter;

    /// <inheritdoc cref="OrchestrationContext.ErrorDataConverter" />
    public JsonDataConverter ErrorDataConverter => _baseContext.ErrorDataConverter;

    /// <inheritdoc cref="OrchestrationContext.OrchestrationInstance" />
    public OrchestrationInstance OrchestrationInstance => _baseContext.OrchestrationInstance;

    public event Action<string, string> Event;
    public Func<string> StatusProvider { get; set; }

    public Guid NewGuid()
    {
        return DeterministicGuid.Create(_orchestrationGuidNamespace, $"{BaseContext.OrchestrationInstance.ExecutionId}/{++_count}");
    }

    internal void RaiseEvent(string name, string input)
    {
        Event?.Invoke(name, input);
    }
}
