using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using LLL.DurableTask.EFCore.Polling;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore
{
    public class EFCoreOrchestrationSession : IOrchestrationSession
    {
        private readonly EFCoreOrchestrationOptions _options;

        private readonly Func<OrchestrationDbContext> _dbContextFactory;
        private readonly CancellationToken _stopCancellationToken;

        public EFCoreOrchestrationSession(
            EFCoreOrchestrationOptions options,
            Func<OrchestrationDbContext> dbContextFactory,
            Instance instance,
            OrchestrationBatch batch,
            Execution execution,
            OrchestrationRuntimeState runtimeState,
            CancellationToken stopCancellationToken)
        {
            _options = options;
            _dbContextFactory = dbContextFactory;
            Instance = instance;
            Batch = batch;
            Execution = execution;
            RuntimeState = runtimeState;
            _stopCancellationToken = stopCancellationToken;
        }

        public Instance Instance { get; }
        public OrchestrationBatch Batch { get; set; }
        public Execution Execution { get; set; }
        public OrchestrationRuntimeState RuntimeState { get; set; }
        public List<OrchestrationMessage> Messages { get; } = new List<OrchestrationMessage>();

        public bool Released { get; set; }

        public async Task<IList<TaskMessage>> FetchNewOrchestrationMessagesAsync(
            TaskOrchestrationWorkItem workItem)
        {
            return await BackoffPollingHelper.PollAsync(async () =>
            {
                using (var dbContext = _dbContextFactory())
                {
                    var messages = await FetchNewMessagesAsync(dbContext);
                    await dbContext.SaveChangesAsync();
                    return messages;
                }
            },
            x => x == null || x.Count > 0,
            _options.FetchNewMessagesPollingTimeout,
            _options.PollingInterval,
            _stopCancellationToken);
        }

        public async Task<IList<TaskMessage>> FetchNewMessagesAsync(
            OrchestrationDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            if (Batch == null)
                return null;

            var dbWorkItems = await dbContext.OrchestrationMessages
                .Where(w => w.AvailableAt <= DateTime.UtcNow
                    && w.BatchId == Batch.Id
                    && w.Batch.LockId == Batch.LockId
                    && !Messages.Contains(w))
                .OrderBy(w => w.AvailableAt)
                .ThenBy(w => w.SequenceNumber)
                .AsNoTracking()
                .ToArrayAsync(cancellationToken);

            var isExecutable = RuntimeState.ExecutionStartedEvent == null
                || RuntimeState.OrchestrationStatus == OrchestrationStatus.Pending
                || RuntimeState.OrchestrationStatus == OrchestrationStatus.Running;

            var messagesToDiscard = dbWorkItems
                .Where(m => !isExecutable || (m.ExecutionId != null && m.ExecutionId != Instance.LastExecutionId))
                .ToArray();

            if (messagesToDiscard.Length > 0)
            {
                foreach (var message in messagesToDiscard)
                {
                    dbContext.OrchestrationMessages.Attach(message);
                    dbContext.OrchestrationMessages.Remove(message);
                }

                dbWorkItems = dbWorkItems
                    .Except(messagesToDiscard)
                    .ToArray();
            }

            Messages.AddRange(dbWorkItems);

            var deserializedMessages = dbWorkItems
                .Select(w => _options.DataConverter.Deserialize<TaskMessage>(w.Message))
                .ToArray();

            return deserializedMessages;
        }

        public void ClearMessages()
        {
            Messages.Clear();
        }
    }
}
