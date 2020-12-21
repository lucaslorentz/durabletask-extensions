using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore.Entities;
using LLL.DurableTask.EFCore.Mappers;
using LLL.DurableTask.EFCore.Polling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore
{
    public abstract partial class EFCoreOrchestrationService :
        IOrchestrationService,
        IExtendedOrchestrationService
    {
        private readonly EFCoreOrchestrationOptions _options;
        private readonly Func<OrchestrationDbContext> _dbContextFactory;
        private readonly OrchestrationMessageMapper _orchestratorMessageMapper;
        private readonly ActivityMessageMapper _activityMessageMapper;
        private readonly InstanceMapper _instanceMapper;
        private readonly ExecutionMapper _executionMapper;
        private readonly ILogger<EFCoreOrchestrationService> _logger;

        private CancellationTokenSource _stopCts = new CancellationTokenSource();

        public EFCoreOrchestrationService(
            IOptions<EFCoreOrchestrationOptions> options,
            Func<OrchestrationDbContext> dbContextFactory,
            OrchestrationMessageMapper orchestratorMessageMapper,
            ActivityMessageMapper activityMessageMapper,
            InstanceMapper instanceMapper,
            ExecutionMapper executionMapper,
            ILogger<EFCoreOrchestrationService> logger)
        {
            _options = options.Value;
            _dbContextFactory = dbContextFactory;
            _orchestratorMessageMapper = orchestratorMessageMapper;
            _activityMessageMapper = activityMessageMapper;
            _instanceMapper = instanceMapper;
            _executionMapper = executionMapper;
            _logger = logger;
        }

        public int TaskOrchestrationDispatcherCount => _options.TaskOrchestrationDispatcherCount;
        public int MaxConcurrentTaskOrchestrationWorkItems => _options.MaxConcurrentTaskOrchestrationWorkItems;
        public int MaxConcurrentTaskActivityWorkItems => _options.MaxConcurrentTaskActivityWorkItems;
        public BehaviorOnContinueAsNew EventBehaviourForContinueAsNew => BehaviorOnContinueAsNew.Carryover;
        public int TaskActivityDispatcherCount => _options.TaskActivityDispatcherCount;

        public Task CreateAsync()
        {
            return CreateAsync(false);
        }

        public async Task CreateAsync(bool recreateInstanceStore)
        {
            using (var dbContext = _dbContextFactory())
            {
                if (recreateInstanceStore)
                    await dbContext.Database.EnsureDeletedAsync();

                await Migrate(dbContext);
            }
        }

        public async Task CreateIfNotExistsAsync()
        {
            using (var dbContext = _dbContextFactory())
            {
                await Migrate(dbContext);
            }
        }

        public Task DeleteAsync()
        {
            return DeleteAsync(false);
        }

        public async Task DeleteAsync(bool deleteInstanceStore)
        {
            using (var dbContext = _dbContextFactory())
            {
                await dbContext.Database.EnsureDeletedAsync();
            }
        }

        public int GetDelayInSecondsAfterOnFetchException(Exception exception)
        {
            return _options.DelayInSecondsAfterFailure;
        }

        public int GetDelayInSecondsAfterOnProcessException(Exception exception)
        {
            return _options.DelayInSecondsAfterFailure;
        }

        public bool IsMaxMessageCountExceeded(int currentMessageCount, OrchestrationRuntimeState runtimeState)
        {
            return false;
        }

        public async Task<TaskOrchestrationWorkItem> LockNextTaskOrchestrationWorkItemAsync(TimeSpan receiveTimeout, CancellationToken cancellationToken)
        {
            return await LockNextTaskOrchestrationWorkItemAsync(receiveTimeout, null, cancellationToken);
        }

        public async Task<TaskOrchestrationWorkItem> LockNextTaskOrchestrationWorkItemAsync(
            TimeSpan receiveTimeout,
            INameVersionInfo[] orchestrations,
            CancellationToken cancellationToken)
        {
            var stoppableCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _stopCts.Token).Token;

            return await BackoffPollingHelper.PollAsync(async () =>
            {
                using (var dbContext = _dbContextFactory())
                {
                    using (var transaction = await BeginTransaction(dbContext))
                    {
                        var instance = await LockNextInstance(dbContext, orchestrations);

                        if (instance == null)
                            return null;

                        var execution = await dbContext.Executions
                            .Where(e => e.InstanceId == instance.InstanceId && e.ExecutionId == instance.LastExecutionId)
                            .FirstOrDefaultAsync();

                        var events = await dbContext.Events
                            .Where(e => e.InstanceId == instance.InstanceId && e.ExecutionId == instance.LastExecutionId)
                            .OrderBy(e => e.SequenceNumber)
                            .AsNoTracking()
                            .ToArrayAsync();

                        var deserializedEvents = events
                            .Select(e => _options.DataConverter.Deserialize<HistoryEvent>(e.Content))
                            .ToArray();

                        var runtimeState = new OrchestrationRuntimeState(deserializedEvents);

                        var session = new EFCoreOrchestrationSession(
                            _options,
                            _dbContextFactory,
                            instance,
                            execution,
                            runtimeState,
                            _stopCts.Token);

                        var messages = await session.FetchNewMessagesAsync(dbContext);

                        if (messages.Count == 0)
                        {
                            await dbContext.SaveChangesAsync();
                            await transaction.CommitAsync();
                            return null;
                        }

                        instance.LockId = Guid.NewGuid().ToString();
                        instance.AvailableAt = DateTime.UtcNow.Add(_options.OrchestrationLockTimeout);

                        await dbContext.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return new TaskOrchestrationWorkItem
                        {
                            InstanceId = instance.InstanceId,
                            LockedUntilUtc = instance.AvailableAt,
                            OrchestrationRuntimeState = runtimeState,
                            NewMessages = messages,
                            Session = session
                        };
                    }
                }
            },
            r => r != null,
            receiveTimeout,
            _options.PollingInterval,
            stoppableCancellationToken);
        }

        public async Task<TaskActivityWorkItem> LockNextTaskActivityWorkItem(TimeSpan receiveTimeout, CancellationToken cancellationToken)
        {
            return await LockNextTaskActivityWorkItem(receiveTimeout, null, cancellationToken);
        }

        public async Task<TaskActivityWorkItem> LockNextTaskActivityWorkItem(
            TimeSpan receiveTimeout,
            INameVersionInfo[] activities,
            CancellationToken cancellationToken)
        {
            var stoppableCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _stopCts.Token).Token;

            return await BackoffPollingHelper.PollAsync(async () =>
            {
                using (var dbContext = _dbContextFactory())
                {
                    using (var transaction = await BeginTransaction(dbContext))
                    {
                        var activityMessage = await LockActivityMessage(dbContext, activities);

                        if (activityMessage == null)
                            return null;

                        activityMessage.LockId = Guid.NewGuid().ToString();
                        activityMessage.AvailableAt = DateTime.UtcNow.Add(_options.ActivtyLockTimeout);

                        await dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return new TaskActivityWorkItem
                        {
                            Id = CreateTaskActivityWorkItemId(activityMessage.Id, activityMessage.LockId),
                            TaskMessage = _options.DataConverter.Deserialize<TaskMessage>(activityMessage.Message),
                            LockedUntilUtc = activityMessage.AvailableAt
                        };
                    }
                }
            },
            x => x != null,
            receiveTimeout,
            _options.PollingInterval,
            stoppableCancellationToken);
        }

        public async Task ReleaseTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;
                if (!session.Released)
                {
                    dbContext.Instances.Attach(session.Instance);
                    session.Instance.LockId = null;
                    session.Instance.AvailableAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                    session.Released = true;
                }
            }
        }

        public async Task AbandonTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;
                if (session.Released)
                    throw new InvalidOperationException("Session was already released");

                dbContext.Instances.Attach(session.Instance);
                session.Instance.LockId = null;
                session.Instance.AvailableAt = DateTime.UtcNow.AddMinutes(1); //TODO: Exponential backoff
                await dbContext.SaveChangesAsync();
                session.Released = true;
            }
        }

        public async Task RenewTaskOrchestrationWorkItemLockAsync(TaskOrchestrationWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;

                var lockedUntilUTC = DateTime.UtcNow.Add(_options.OrchestrationLockTimeout);

                dbContext.Instances.Attach(session.Instance);
                session.Instance.AvailableAt = DateTime.UtcNow.AddMinutes(5);
                await dbContext.SaveChangesAsync();

                workItem.LockedUntilUtc = lockedUntilUTC;
            }
        }

        public async Task AbandonTaskActivityWorkItemAsync(TaskActivityWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory())
            {
                var (id, lockId) = ParseTaskActivityWorkItemId(workItem.Id);

                await ReleaseActivityMessageLock(dbContext, id, lockId);
            }
        }

        public async Task<TaskActivityWorkItem> RenewTaskActivityWorkItemLockAsync(TaskActivityWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory())
            {
                var (id, lockId) = ParseTaskActivityWorkItemId(workItem.Id);

                var lockedUntilUTC = DateTime.UtcNow.Add(_options.ActivtyLockTimeout);

                var renewedCount = await RenewActivityMessageLock(dbContext, id, lockId, lockedUntilUTC);

                if (renewedCount == 0)
                    throw new Exception("Lost task activity lock");

                workItem.LockedUntilUtc = lockedUntilUTC;

                return workItem;
            }
        }

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return StopAsync(false);
        }

        public Task StopAsync(bool isForced)
        {
            _stopCts.Cancel();
            _stopCts = new CancellationTokenSource();
            return Task.CompletedTask;
        }

        public async Task CompleteTaskOrchestrationWorkItemAsync(
            TaskOrchestrationWorkItem workItem,
            OrchestrationRuntimeState newOrchestrationRuntimeState,
            IList<TaskMessage> outboundMessages,
            IList<TaskMessage> orchestratorMessages,
            IList<TaskMessage> timerMessages,
            TaskMessage continuedAsNewMessage,
            OrchestrationState orchestrationState)
        {
            using (var dbContext = _dbContextFactory())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;

                // Create child orchestrations
                foreach (var executionStartedEvent in orchestratorMessages.Select(m => m.Event).OfType<ExecutionStartedEvent>())
                {
                    var childInstance = _instanceMapper.CreateInstance(executionStartedEvent);
                    await dbContext.Instances.AddAsync(childInstance);

                    var childRuntimeState = new OrchestrationRuntimeState(new[] { executionStartedEvent });
                    var childExecution = _executionMapper.CreateExecution(childRuntimeState);
                    await dbContext.Executions.AddAsync(childExecution);
                }

                // Write messages
                var activityMessages = outboundMessages.Select(_activityMessageMapper.CreateActivityMessage).ToArray();
                var orchestatorMessages = orchestratorMessages.Select(_orchestratorMessageMapper.CreateOrchestratorMessage).ToArray();
                var timerOrchestratorMessages = timerMessages.Select(_orchestratorMessageMapper.CreateOrchestratorMessage).ToArray();
                var newOrchestratorWorkItem = continuedAsNewMessage != null
                    ? _orchestratorMessageMapper.CreateOrchestratorMessage(continuedAsNewMessage, 0)
                    : null;

                await dbContext.ActivityMessages.AddRangeAsync(activityMessages);
                await dbContext.OrchestratorMessages.AddRangeAsync(orchestatorMessages);
                await dbContext.OrchestratorMessages.AddRangeAsync(timerOrchestratorMessages);

                if (newOrchestratorWorkItem != null)
                    await dbContext.OrchestratorMessages.AddAsync(newOrchestratorWorkItem);

                // Remove executed messages
                dbContext.AttachRange(session.Messages);
                dbContext.OrchestratorMessages.RemoveRange(session.Messages);

                // Update instance
                var instance = session.Instance;
                dbContext.Instances.Attach(instance);
                _instanceMapper.UpdateInstance(instance, newOrchestrationRuntimeState);

                // Update current execution
                session.Execution = await SaveExecutionAsync(dbContext, workItem.OrchestrationRuntimeState, session.Execution);

                // Update new execution
                EnrichNewEventsInput(newOrchestrationRuntimeState, outboundMessages, orchestratorMessages);

                if (newOrchestrationRuntimeState != workItem.OrchestrationRuntimeState)
                    session.Execution = await SaveExecutionAsync(dbContext, newOrchestrationRuntimeState);

                await dbContext.SaveChangesAsync();

                session.RuntimeState = newOrchestrationRuntimeState;
                session.ClearMessages();
            }
        }

        public async Task CompleteTaskActivityWorkItemAsync(TaskActivityWorkItem workItem, TaskMessage responseMessage)
        {
            using (var dbContext = _dbContextFactory())
            {
                var (id, lockId) = ParseTaskActivityWorkItemId(workItem.Id);

                var dbWorkItem = await dbContext.ActivityMessages
                    .FirstAsync(w => w.Id == id && w.LockId == lockId);

                var orchestratorWorkItem = _orchestratorMessageMapper.CreateOrchestratorMessage(responseMessage, 0);

                dbContext.ActivityMessages.Remove(dbWorkItem);

                await dbContext.OrchestratorMessages.AddAsync(orchestratorWorkItem);
                await dbContext.SaveChangesAsync();
            }
        }

        protected abstract Task Migrate(OrchestrationDbContext dbContext);

        protected abstract Task<IDbContextTransaction> BeginTransaction(OrchestrationDbContext dbContext);

        protected abstract Task<Instance> LockAnyQueueInstance(OrchestrationDbContext dbContext);

        protected abstract Task<Instance> LockQueuesInstance(OrchestrationDbContext dbContext, string[] queues);

        protected abstract Task<ActivityMessage> LockAnyQueueActivityMessage(OrchestrationDbContext dbContext);

        protected abstract Task<ActivityMessage> LockQueuesActivityMessage(OrchestrationDbContext dbContext, string[] queues);

        protected abstract Task<int> RenewActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId, DateTime lockedUntilUTC);

        protected abstract Task<int> ReleaseActivityMessageLock(OrchestrationDbContext dbContext, Guid id, string lockId);

        private async Task<Execution> SaveExecutionAsync(
            OrchestrationDbContext dbContext,
            OrchestrationRuntimeState runtimeState,
            Execution existingExecution = null)
        {
            Execution execution;

            if (existingExecution == null)
            {
                execution = _executionMapper.CreateExecution(runtimeState);
                await dbContext.Executions.AddAsync(execution);
            }
            else
            {
                execution = existingExecution;
                dbContext.Executions.Attach(execution);
                _executionMapper.UpdateExecution(execution, runtimeState);
            }

            var initialSequenceNumber = runtimeState.Events.Count - runtimeState.NewEvents.Count;

            var newEvents = runtimeState.NewEvents
                .Select((e, i) => new Event
                {
                    Id = Guid.NewGuid(),
                    InstanceId = runtimeState.OrchestrationInstance.InstanceId,
                    ExecutionId = runtimeState.OrchestrationInstance.ExecutionId,
                    SequenceNumber = initialSequenceNumber + i,
                    Content = _options.DataConverter.Serialize(e)
                }).ToArray();

            await dbContext.Events.AddRangeAsync(newEvents);

            return execution;
        }

        private async Task<Instance> LockNextInstance(OrchestrationDbContext dbContext, INameVersionInfo[] orchestrations)
        {
            if (orchestrations == null)
                return await LockAnyQueueInstance(dbContext);

            var queues = orchestrations
                .Select(nv => QueueMapper.ToQueueName(nv.Name, nv.Version))
                .ToArray();

            var instance = await LockQueuesInstance(dbContext, queues);
            if (instance != null)
                return instance;

            return null;
        }

        private async Task<ActivityMessage> LockActivityMessage(OrchestrationDbContext dbContext, INameVersionInfo[] activities)
        {
            if (activities == null)
                return await LockAnyQueueActivityMessage(dbContext);

            var queues = activities
                .Select(nv => QueueMapper.ToQueueName(nv.Name, nv.Version))
                .ToArray();

            var activityMessage = await LockQueuesActivityMessage(dbContext, queues);
            if (activityMessage != null)
                return activityMessage;

            return null;
        }

        private static string CreateTaskActivityWorkItemId(Guid id, string lockId)
        {
            return $"{id}|{lockId}";
        }

        private static (Guid id, string lockId) ParseTaskActivityWorkItemId(string id)
        {
            var parts = id.Split('|');
            return (Guid.Parse(parts[0]), parts[1]);
        }

        private static void EnrichNewEventsInput(OrchestrationRuntimeState newOrchestrationRuntimeState, IList<TaskMessage> outboundMessages, IList<TaskMessage> orchestratorMessages)
        {
            foreach (var outboundEvent in outboundMessages.Select(e => e.Event))
            {
                switch (outboundEvent)
                {
                    case TaskScheduledEvent outboundTaskScheduledEvent:
                        foreach (var taskScheduledEvent in newOrchestrationRuntimeState.NewEvents.OfType<TaskScheduledEvent>())
                        {
                            if (taskScheduledEvent.EventId == outboundTaskScheduledEvent.EventId)
                            {
                                taskScheduledEvent.Input = outboundTaskScheduledEvent.Input;
                            }
                        }
                        break;
                }
            }
            foreach (var orchestratorEvent in orchestratorMessages.Select(e => e.Event))
            {
                switch (orchestratorEvent)
                {
                    case ExecutionStartedEvent executionStartedEvent:
                        foreach (var subOrchestrationCreatedEvent in newOrchestrationRuntimeState.NewEvents.OfType<SubOrchestrationInstanceCreatedEvent>())
                        {
                            if (subOrchestrationCreatedEvent.InstanceId == executionStartedEvent.OrchestrationInstance.InstanceId)
                            {
                                subOrchestrationCreatedEvent.Input = executionStartedEvent.Input;
                            }
                        }
                        break;
                }
            }
        }
    }
}
