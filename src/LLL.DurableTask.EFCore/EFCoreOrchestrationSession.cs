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
            .ToArrayAsync(cancellationToken);

        var messagesToDiscard = newDbMessages
            .Where(m => m.ExecutionId is not null && m.ExecutionId != Instance.LastExecutionId)
            .ToArray();

        if (messagesToDiscard.Length > 0)
        {
            foreach (var message in messagesToDiscard)
            {
                dbContext.OrchestrationMessages.Attach(message);
                dbContext.OrchestrationMessages.Remove(message);
            }

            newDbMessages = newDbMessages
                .Except(messagesToDiscard)
                .ToArray();
        }

        var deserializedMessages = newDbMessages
            .Select(w => _options.DataConverter.Deserialize<TaskMessage>(w.Message))
            .ToList();

        if (RuntimeState.ExecutionStartedEvent is not null)
        {
            if (RuntimeState.OrchestrationStatus is OrchestrationStatus.Completed
                && deserializedMessages.Any(m => m.Event.EventType == EventType.EventRaised))
            {
                // Reopen completed orchestrations after receiving an event raised
                RuntimeState = new OrchestrationRuntimeState(
                    RuntimeState.Events.Reopen(_options.DataConverter)
                );
            }

            var isRunning = RuntimeState.OrchestrationStatus is OrchestrationStatus.Running
                    or OrchestrationStatus.Suspended
                    or OrchestrationStatus.Pending;

            if (!isRunning)
            {
                // Discard all messages if not running
                foreach (var message in newDbMessages)
                {
                    dbContext.OrchestrationMessages.Attach(message);
                    dbContext.OrchestrationMessages.Remove(message);
                }
                newDbMessages = [];
                deserializedMessages = [];
            }
        }

        Messages.AddRange(newDbMessages);

        return deserializedMessages;
    }

    public void ClearMessages()
    {
        Messages.Clear();
    }
}
