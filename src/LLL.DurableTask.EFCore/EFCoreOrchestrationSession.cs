using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using LLL.DurableTask.EFCore.Polling;
using Microsoft.EntityFrameworkCore;
using NATS.Client;

namespace LLL.DurableTask.EFCore
{
    public class EFCoreOrchestrationSession : IOrchestrationSession
    {
        private readonly EFCoreOrchestrationOptions _options;

        private readonly IDbContextFactory<OrchestrationDbContext> _dbContextFactory;
        private readonly CancellationToken _stopCancellationToken;
        private readonly IAsyncSubscription _subscription;

        public EFCoreOrchestrationSession(
            EFCoreOrchestrationOptions options,
            IDbContextFactory<OrchestrationDbContext> dbContextFactory,
            Instance instance,
            Execution execution,
            OrchestrationRuntimeState runtimeState,
            CancellationToken stopCancellationToken,
            IAsyncSubscription subscription)
        {
            _options = options;
            _dbContextFactory = dbContextFactory;
            Instance = instance;
            Execution = execution;
            RuntimeState = runtimeState;
            _stopCancellationToken = stopCancellationToken;
            _subscription = subscription;
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
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    var messages = await FetchNewMessagesAsync(dbContext);
                    await dbContext.SaveChangesAsync();
                    return messages;
                }
            },
            x => x == null || x.Count > 0,
            _options.FetchNewMessagesPollingTimeout,
            _options.PollingInterval,
            _stopCancellationToken,
            BackoffPollingHelper.CreateNatsWaitUntilSignal(
                _subscription,
                new HashSet<string> { $"orchestration.{Instance.LastQueue}.{Instance.InstanceId}" }));
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
                .Where(m => m.ExecutionId != null && m.ExecutionId != Instance.LastExecutionId)
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

            Messages.AddRange(newDbMessages);

            var deserializedMessages = newDbMessages
                .Select(w => _options.DataConverter.Deserialize<TaskMessage>(w.Message))
                .ToList();

            return deserializedMessages;
        }

        public void ClearMessages()
        {
            Messages.Clear();
        }
    }
}
