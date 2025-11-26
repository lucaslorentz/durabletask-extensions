using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.EFCore.Entities;
using LLL.DurableTask.EFCore.Extensions;
using LLL.DurableTask.EFCore.Polling;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore;

public class EFCoreOrchestrationSession : IOrchestrationSession
{
    private readonly EFCoreOrchestrationOptions _options;

    private readonly IDbContextFactory<OrchestrationDbContext> _dbContextFactory;
    private readonly CancellationToken _stopCancellationToken;

    public EFCoreOrchestrationSession(
        EFCoreOrchestrationOptions options,
        IDbContextFactory<OrchestrationDbContext> dbContextFactory,
        Instance instance,
        Execution execution,
        OrchestrationRuntimeState runtimeState,
        CancellationToken stopCancellationToken)
    {
        _options = options;
        _dbContextFactory = dbContextFactory;
        Instance = instance;
        Execution = execution;
        RuntimeState = runtimeState;
        _stopCancellationToken = stopCancellationToken;
    }

    public Instance Instance { get; }
    public Execution Execution { get; set; }
    public OrchestrationRuntimeState RuntimeState { get; set; }
    public List<OrchestrationMessage> Messages { get; } = new List<OrchestrationMessage>();

    public bool Released { get; set; }

    public async Task<IList<TaskMessage>> FetchNewOrchestrationMessagesAsync(
        TaskOrchestrationWorkItem workItem)
    {
        return await BackoffPollingHelper.PollAsync(async () =>
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var messages = await FetchNewMessagesAsync(dbContext);
            await dbContext.SaveChangesAsync();
            return messages;
        },
        x => x is null || x.Count > 0,
        _options.FetchNewMessagesPollingTimeout,
        _options.PollingInterval,
        _stopCancellationToken);
    }

    public async Task<IList<TaskMessage>> FetchNewMessagesAsync(
        OrchestrationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var newDbMessages = await dbContext.OrchestrationMessages
            .Where(w => w.AvailableAt <= DateTime.UtcNow
                && w.InstanceId == Instance.InstanceId
                && w.Instance.LockId == Instance.LockId // Ensure we still own the lock
                && !Messages.Contains(w))
            .OrderBy(w => w.AvailableAt)
            .ThenBy(w => w.SequenceNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var deserializedMessages = newDbMessages
            .Select(w => _options.DataConverter.Deserialize<TaskMessage>(w.Message))
            .ToList();

        if (RuntimeState.ExecutionStartedEvent is not null
            && RuntimeState.OrchestrationStatus is OrchestrationStatus.Completed
            && deserializedMessages.Any(m => m.Event.EventType == EventType.EventRaised))
        {
            // Reopen completed orchestrations after receiving an event raised
            RuntimeState = new OrchestrationRuntimeState(
                RuntimeState.Events.Reopen(_options.DataConverter)
            );
        }

        var isRunning = RuntimeState.ExecutionStartedEvent is null
            || RuntimeState.OrchestrationStatus is OrchestrationStatus.Running
                or OrchestrationStatus.Suspended
                or OrchestrationStatus.Pending;

        for (var i = newDbMessages.Count - 1; i >= 0; i--)
        {
            var dbMessage = newDbMessages[i];
            var deserializedMessage = deserializedMessages[i];

            if (ShouldDropNewMessage(isRunning, dbMessage, deserializedMessage))
            {
                dbContext.OrchestrationMessages.Attach(dbMessage);
                dbContext.OrchestrationMessages.Remove(dbMessage);
                newDbMessages.RemoveAt(i);
                deserializedMessages.RemoveAt(i);
            }
        }

        Messages.AddRange(newDbMessages);

        return deserializedMessages;
    }

    private bool ShouldDropNewMessage(
        bool isRunning,
        OrchestrationMessage dbMessage,
        TaskMessage taskMessage)
    {
        // Drop messages to previous executions
        if (dbMessage.ExecutionId is not null && dbMessage.ExecutionId != Instance.LastExecutionId)
            return true;

        // When not running, drop anything that is not execution rewound
        if (!isRunning && taskMessage.Event.EventType != EventType.ExecutionRewound)
            return true;

        return false;
    }

    public void ClearMessages()
    {
        Messages.Clear();
    }
}
