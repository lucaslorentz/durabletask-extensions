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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore
{
    public partial class EFCoreOrchestrationService :
        IOrchestrationService,
        IExtendedOrchestrationService
    {
        private readonly EFCoreOrchestrationOptions _options;
        private readonly IDbContextFactory<OrchestrationDbContext> _dbContextFactory;
        private readonly OrchestrationDbContextExtensions _dbContextExtensions;
        private readonly OrchestrationMessageMapper _orchestrationMessageMapper;
        private readonly ActivityMessageMapper _activityMessageMapper;
        private readonly InstanceMapper _instanceMapper;
        private readonly ExecutionMapper _executionMapper;
        private readonly ILogger<EFCoreOrchestrationService> _logger;

        private CancellationTokenSource _stopCts = new CancellationTokenSource();

        public EFCoreOrchestrationService(
            IOptions<EFCoreOrchestrationOptions> options,
            IDbContextFactory<OrchestrationDbContext> dbContextFactory,
            OrchestrationDbContextExtensions dbContextExtensions,
            OrchestrationMessageMapper orchestrationMessageMapper,
            ActivityMessageMapper activityMessageMapper,
            InstanceMapper instanceMapper,
            ExecutionMapper executionMapper,
            ILogger<EFCoreOrchestrationService> logger)
        {
            _options = options.Value;
            _dbContextFactory = dbContextFactory;
            _dbContextExtensions = dbContextExtensions;
            _orchestrationMessageMapper = orchestrationMessageMapper;
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                if (recreateInstanceStore)
                    await dbContext.Database.EnsureDeletedAsync();

                await _dbContextExtensions.Migrate(dbContext);
            }
        }

        public async Task CreateIfNotExistsAsync()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                await _dbContextExtensions.Migrate(dbContext);
            }
        }

        public Task DeleteAsync()
        {
            return DeleteAsync(false);
        }

        public async Task DeleteAsync(bool deleteInstanceStore)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
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
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    var instance = await LockNextInstanceAsync(dbContext, orchestrations);

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
                        instance.LockId = null;
                        instance.LockedUntil = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                        return null;
                    }

                    await dbContext.SaveChangesAsync();

                    return new TaskOrchestrationWorkItem
                    {
                        InstanceId = instance.InstanceId,
                        LockedUntilUtc = instance.LockedUntil,
                        OrchestrationRuntimeState = runtimeState,
                        NewMessages = messages,
                        Session = session
                    };
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
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    var activityMessage = await LockActivityMessage(dbContext, activities);

                    if (activityMessage == null)
                        return null;

                    return new TaskActivityWorkItem
                    {
                        Id = CreateTaskActivityWorkItemId(activityMessage.Id, activityMessage.LockId, activityMessage.ReplyQueue),
                        TaskMessage = _options.DataConverter.Deserialize<TaskMessage>(activityMessage.Message),
                        LockedUntilUtc = activityMessage.LockedUntil
                    };
                }
            },
            x => x != null,
            receiveTimeout,
            _options.PollingInterval,
            stoppableCancellationToken);
        }

        public async Task ReleaseTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;
                if (!session.Released)
                {
                    dbContext.Instances.Attach(session.Instance);
                    session.Instance.LockId = null;
                    session.Instance.LockedUntil = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                    session.Released = true;
                }
            }
        }

        public async Task AbandonTaskOrchestrationWorkItemAsync(TaskOrchestrationWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;
                if (session.Released)
                    throw new InvalidOperationException("Session was already released");

                dbContext.Instances.Attach(session.Instance);
                session.Instance.LockId = null;
                // TODO: Exponential backoff
                session.Instance.LockedUntil = DateTime.UtcNow.AddMinutes(1);
                await dbContext.SaveChangesAsync();
                session.Released = true;
            }
        }

        public async Task RenewTaskOrchestrationWorkItemLockAsync(TaskOrchestrationWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;

                var lockedUntilUTC = DateTime.UtcNow.Add(_options.OrchestrationLockTimeout);
                dbContext.Instances.Attach(session.Instance);
                session.Instance.LockedUntil = DateTime.UtcNow.Add(_options.OrchestrationLockTimeout);
                await dbContext.SaveChangesAsync();

                workItem.LockedUntilUtc = lockedUntilUTC;
            }
        }

        public async Task AbandonTaskActivityWorkItemAsync(TaskActivityWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var (id, lockId, _) = ParseTaskActivityWorkItemId(workItem.Id);

                var activityMessage = await dbContext.ActivityMessages.FindAsync(id);

                if (activityMessage.LockId != lockId)
                    throw new Exception("Lost task activity lock");

                activityMessage.LockId = null;
                activityMessage.LockedUntil = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<TaskActivityWorkItem> RenewTaskActivityWorkItemLockAsync(TaskActivityWorkItem workItem)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var (id, lockId, _) = ParseTaskActivityWorkItemId(workItem.Id);

                var activityMessage = await dbContext.ActivityMessages.FindAsync(id);

                if (activityMessage.LockId != lockId)
                    throw new Exception("Lost task activity lock");

                var lockedUntilUTC = DateTime.UtcNow.Add(_options.ActivtyLockTimeout);
                activityMessage.LockedUntil = lockedUntilUTC;
                await dbContext.SaveChangesAsync();

                workItem.LockedUntilUtc = lockedUntilUTC;

                return workItem;
            }
        }

        public Task StartAsync()
        {
            if (_stopCts.IsCancellationRequested)
                _stopCts = new CancellationTokenSource();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return StopAsync(false);
        }

        public Task StopAsync(bool isForced)
        {
            _stopCts.Cancel();
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
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var session = workItem.Session as EFCoreOrchestrationSession;

                await _dbContextExtensions.WithinTransaction(dbContext, async () =>
                {
                    dbContext.Instances.Attach(session.Instance);

                    // Create child orchestrations
                    foreach (var executionStartedEvent in orchestratorMessages.Select(m => m.Event).OfType<ExecutionStartedEvent>())
                    {
                        var childInstance = _instanceMapper.CreateInstance(executionStartedEvent);
                        await dbContext.Instances.AddAsync(childInstance);

                        var childRuntimeState = new OrchestrationRuntimeState(new[] { executionStartedEvent });
                        var childExecution = _executionMapper.CreateExecution(childRuntimeState);
                        await dbContext.Executions.AddAsync(childExecution);
                    }

                    var orchestrationQueueName = QueueMapper.ToQueue(orchestrationState.Name, orchestrationState.Version);

                    var knownQueues = new Dictionary<string, string>
                    {
                        [orchestrationState.OrchestrationInstance.InstanceId] = orchestrationQueueName
                    };

                    if (orchestrationState.ParentInstance != null)
                        knownQueues[orchestrationState.ParentInstance.OrchestrationInstance.InstanceId] = QueueMapper.ToQueue(orchestrationState.ParentInstance.Name, orchestrationState.ParentInstance.Version);

                    // Write messages
                    var activityMessages = outboundMessages
                        .Select(m => _activityMessageMapper.CreateActivityMessage(m, orchestrationQueueName))
                        .ToArray();

                    await dbContext.ActivityMessages.AddRangeAsync(activityMessages);

                    var allOrchestrationMessages = orchestratorMessages
                        .Concat(timerMessages)
                        .Concat(continuedAsNewMessage != null ? new[] { continuedAsNewMessage } : Enumerable.Empty<TaskMessage>())
                        .ToArray();

                    await SendTaskOrchestrationMessagesAsync(dbContext, allOrchestrationMessages, knownQueues);

                    // Remove executed messages
                    dbContext.AttachRange(session.Messages);
                    dbContext.OrchestrationMessages.RemoveRange(session.Messages);

                    // Update instance
                    _instanceMapper.UpdateInstance(session.Instance, newOrchestrationRuntimeState);

                    // Update current execution
                    EnrichNewEventsInput(workItem.OrchestrationRuntimeState, outboundMessages, orchestratorMessages);
                    session.Execution = await SaveExecutionAsync(dbContext, workItem.OrchestrationRuntimeState, session.Execution);

                    // Update new execution
                    if (newOrchestrationRuntimeState != workItem.OrchestrationRuntimeState)
                    {
                        EnrichNewEventsInput(newOrchestrationRuntimeState, outboundMessages, orchestratorMessages);
                        session.Execution = await SaveExecutionAsync(dbContext, newOrchestrationRuntimeState);
                    }

                    await dbContext.SaveChangesAsync();
                });

                session.RuntimeState = newOrchestrationRuntimeState;
                session.ClearMessages();
            }
        }

        public async Task CompleteTaskActivityWorkItemAsync(TaskActivityWorkItem workItem, TaskMessage responseMessage)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                await _dbContextExtensions.WithinTransaction(dbContext, async () =>
                {
                    var (id, lockId, orchestrationQueue) = ParseTaskActivityWorkItemId(workItem.Id);

                    var activityMessage = await dbContext.ActivityMessages
                        .FirstAsync(w => w.Id == id && w.LockId == lockId);

                    dbContext.ActivityMessages.Remove(activityMessage);

                    var knownQueues = new Dictionary<string, string>
                    {
                        [workItem.TaskMessage.OrchestrationInstance.InstanceId] = orchestrationQueue
                    };

                    await SendTaskOrchestrationMessagesAsync(dbContext, new[] { responseMessage }, knownQueues);

                    await dbContext.SaveChangesAsync();
                });
            }
        }

        private async Task SendTaskOrchestrationMessagesAsync(
            OrchestrationDbContext dbContext,
            TaskMessage[] messages,
            IDictionary<string, string> knownQueues = null)
        {
            var instancesGroups = messages
                .GroupBy(m => m.OrchestrationInstance.InstanceId)
                .ToArray();

            foreach (var instanceGroup in instancesGroups)
            {
                if (knownQueues == null || !knownQueues.TryGetValue(instanceGroup.Key, out var queue))
                {
                    var instance = await dbContext.Instances.FindAsync(instanceGroup.Key);

                    if (instance == null)
                        throw new Exception($"Instance {instanceGroup.Key} not found");

                    queue = instance.LastQueue;
                }

                var orchestrationMessages = instanceGroup
                    .Select((m, i) => _orchestrationMessageMapper.CreateOrchestrationMessageAsync(m, i, queue))
                    .ToArray();

                await dbContext.OrchestrationMessages.AddRangeAsync(orchestrationMessages);
            }
        }

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

        private async Task<Instance> LockNextInstanceAsync(OrchestrationDbContext dbContext, INameVersionInfo[] orchestrations)
        {
            if (orchestrations == null)
                return await _dbContextExtensions.TryLockNextInstanceAsync(dbContext, _options.OrchestrationLockTimeout);

            var queues = orchestrations
                .Select(QueueMapper.ToQueue)
                .ToArray();

            var instance = await _dbContextExtensions.TryLockNextInstanceAsync(dbContext, queues, _options.OrchestrationLockTimeout);
            if (instance != null)
                return instance;

            return null;
        }

        private async Task<ActivityMessage> LockActivityMessage(OrchestrationDbContext dbContext, INameVersionInfo[] activities)
        {
            var lockId = Guid.NewGuid().ToString();
            var lockUntilUtc = DateTime.UtcNow.Add(_options.OrchestrationLockTimeout);

            if (activities == null)
                return await _dbContextExtensions.TryLockNextActivityMessageAsync(dbContext, _options.OrchestrationLockTimeout);

            var queues = activities
                .Select(QueueMapper.ToQueue)
                .ToArray();

            var activityMessage = await _dbContextExtensions.TryLockNextActivityMessageAsync(dbContext, queues, _options.OrchestrationLockTimeout);
            if (activityMessage != null)
                return activityMessage;

            return null;
        }

        private static string CreateTaskActivityWorkItemId(Guid id, string lockId, string replyQueue)
        {
            return $"{id}|{lockId}|{replyQueue}";
        }

        private static (Guid id, string lockId, string replyQueue) ParseTaskActivityWorkItemId(string id)
        {
            var parts = id.Split('|');
            return (Guid.Parse(parts[0]), parts[1], parts[2]);
        }

        private static void EnrichNewEventsInput(OrchestrationRuntimeState orchestrationRuntimeState, IList<TaskMessage> outboundMessages, IList<TaskMessage> orchestratorMessages)
        {
            foreach (var outboundEvent in outboundMessages.Select(e => e.Event))
            {
                switch (outboundEvent)
                {
                    case TaskScheduledEvent outboundTaskScheduledEvent:
                        foreach (var taskScheduledEvent in orchestrationRuntimeState.NewEvents.OfType<TaskScheduledEvent>())
                        {
                            if (taskScheduledEvent.EventId == outboundTaskScheduledEvent.EventId)
                            {
                                taskScheduledEvent.Input = outboundTaskScheduledEvent.Input;
                            }
                        }
                        break;
                }
            }
            foreach (var orchestrationEvent in orchestratorMessages.Select(e => e.Event))
            {
                switch (orchestrationEvent)
                {
                    case ExecutionStartedEvent executionStartedEvent:
                        foreach (var subOrchestrationCreatedEvent in orchestrationRuntimeState.NewEvents.OfType<SubOrchestrationInstanceCreatedEvent>())
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
