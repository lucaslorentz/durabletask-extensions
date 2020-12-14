using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore
{
    public class EFCoreOrchestrationSession : IOrchestrationSession
    {
        private readonly static TimeSpan _fetchNewMessagesPollingTimeout = TimeSpan.FromSeconds(5);
        private readonly static TimeSpan _fetchNewMessagesPollingInterval = TimeSpan.FromMilliseconds(100);

        private readonly DataConverter _dataConverter = new JsonDataConverter();

        private readonly Func<OrchestrationDbContext> _dbContextFactory;
        private readonly CancellationToken _stopCancellationToken;

        public EFCoreOrchestrationSession(
            Func<OrchestrationDbContext> dbContextFactory,
            Instance instance,
            Execution execution,
            OrchestrationRuntimeState runtimeState,
            CancellationToken stopCancellationToken)
        {
            _dbContextFactory = dbContextFactory;
            Instance = instance;
            Execution = execution;
            RuntimeState = runtimeState;
            _stopCancellationToken = stopCancellationToken;
        }

        public Instance Instance { get; }
        public Execution Execution { get; set; }
        public OrchestrationRuntimeState RuntimeState { get; set; }
        public List<OrchestratorMessage> Messages { get; } = new List<OrchestratorMessage>();

        public bool Released { get; set; }

        public async Task<IList<TaskMessage>> FetchNewOrchestrationMessagesAsync(
            TaskOrchestrationWorkItem workItem)
        {
            var stoppableCts = CancellationTokenSource.CreateLinkedTokenSource(_stopCancellationToken);
            stoppableCts.CancelAfter(_fetchNewMessagesPollingTimeout);

            return await PollingHelper.PollAsync(async () =>
            {
                using (var dbContext = _dbContextFactory())
                {
                    var messages = await FetchNewMessagesAsync(dbContext);
                    await dbContext.SaveChangesAsync();
                    return messages;
                }
            }, x => x.Count > 0, _fetchNewMessagesPollingInterval, stoppableCts.Token);
        }

        public async Task<IList<TaskMessage>> FetchNewMessagesAsync(
            OrchestrationDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            var dbWorkItems = await dbContext.OrchestratorMessages
                .Where(w => w.Instance.InstanceId == Instance.InstanceId && w.Instance.LockId == Instance.LockId)
                .Where(w => w.AvailableAt <= DateTime.UtcNow)
                .Where(w => !Messages.Contains(w))
                .OrderBy(w => w.AvailableAt).ThenBy(w => w.SequenceNumber)
                .AsNoTracking()
                .ToArrayAsync(cancellationToken);

            var isExecutable = RuntimeState.OrchestrationStatus == OrchestrationStatus.Pending
                || RuntimeState.OrchestrationStatus == OrchestrationStatus.Running;

            var messagesToDiscard = dbWorkItems
                .Where(m => !isExecutable || (m.ExecutionId != null && m.ExecutionId != Instance.LastExecutionId))
                .ToArray();

            if (messagesToDiscard.Length > 0)
            {
                foreach (var message in messagesToDiscard)
                {
                    dbContext.OrchestratorMessages.Attach(message);
                    dbContext.OrchestratorMessages.Remove(message);
                }

                dbWorkItems = dbWorkItems
                    .Except(messagesToDiscard)
                    .ToArray();
            }

            Messages.AddRange(dbWorkItems);

            var deserializedMessages = dbWorkItems
                .Select(w => _dataConverter.Deserialize<TaskMessage>(w.Message))
                .ToArray();

            return deserializedMessages;
        }

        public void ClearMessages()
        {
            Messages.Clear();
        }
    }
}
